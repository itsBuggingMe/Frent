using Frent.Buffers;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype
{
    internal ArchetypeID ID => _archetypeID;
    internal ImmutableArray<ComponentID> ArchetypeTypeArray => _archetypeID.Types;
    internal ImmutableArray<TagID> ArchetypeTagArray => _archetypeID.Tags;
    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Type.Name))} Tags: {string.Join(", ", ArchetypeTagArray.Select(t => t.Type.Name))}";
    internal int EntityCount => _nextComponentIndex;
    internal Span<T> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GetComponentIndex<T>();
        if (index == 0)
        {
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
        }
        return UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(index)).AsSpanLength(_nextComponentIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref T GetComponentDataReference<T>()
    {
        int index = GetComponentIndex<T>();
        return ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(Components.UnsafeArrayIndex(index)).GetComponentStorageDataReference();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EntityIDOnly CreateEntityLocation(EntityFlags flags, out EntityLocation entityLocation)
    {
        if (_entities.Length == _nextComponentIndex)
            Resize(_entities.Length * 2);

        entityLocation.Archetype = this;
        entityLocation.Index = _nextComponentIndex;
        entityLocation.Flags = flags;

        return ref _entities.UnsafeArrayIndex(_nextComponentIndex++);
    }

    /// <summary>
    /// Caller needs write archetype field
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EntityIDOnly CreateDeferredEntityLocation(World world, scoped ref EntityLocation entityLocation, out int physicalIndex, out ComponentStorageBase[] writeStorage)
    {
        if (_deferredEntityCount == 0)
            world.DeferredCreationArchetypes.Push(this);

        int futureSlot = _nextComponentIndex + _deferredEntityCount++;
        entityLocation.Index = futureSlot;

        if (futureSlot < _entities.Length)
        {//hot path: we have space and can directly place into existing array
            writeStorage = Components;
            physicalIndex = futureSlot;
            return ref _entities.UnsafeArrayIndex(physicalIndex);
        }

        //we need to place into temp buffers
        physicalIndex = futureSlot - _entities.Length;
        Debug.Assert(physicalIndex >= 0);
        if (physicalIndex >= _createComponentBufferEntities.Length)
        {
            ResizeCreateComponentBuffers();
        }

        writeStorage = CreateComponentBuffers;

        return ref _createComponentBufferEntities.UnsafeArrayIndex(physicalIndex);
    }

    internal void ResolveDeferredEntityCreations(World world)
    {
        Debug.Assert(_deferredEntityCount != 0);
        int deltaFromMaxDeferredInPlace = -(_entities.Length - (_nextComponentIndex + _deferredEntityCount));
        int previousComponentCount = _nextComponentIndex;

        if (!(deltaFromMaxDeferredInPlace <= 0))
        {//components overflowed into temp storage

            int oldEntitiesLen = _entities.Length;
            int totalCapacityRequired = previousComponentCount + _deferredEntityCount;
            Debug.Assert(totalCapacityRequired >= oldEntitiesLen);

            //we should always have to resize here - after all, no space is left
            Resize((int)BitOperations.RoundUpToPowerOf2((uint)totalCapacityRequired));
            var destination = Components;
            var source = CreateComponentBuffers;
            for (int i = 1; i < destination.Length; i++)
                Array.Copy(source[i].Buffer, 0, destination[i].Buffer, oldEntitiesLen, deltaFromMaxDeferredInPlace);
            Array.Copy(_createComponentBufferEntities, 0, _entities, oldEntitiesLen, deltaFromMaxDeferredInPlace);
        }

        _nextComponentIndex += _deferredEntityCount;

        var entities = _entities;
        var table = world.EntityTable._buffer;
        for (int i = previousComponentCount; i < entities.Length && i < _nextComponentIndex; i++)
            table.UnsafeArrayIndex(entities[i].ID).Location.Archetype = this;

        _deferredEntityCount = 0;
    }

    internal Span<EntityIDOnly> CreateEntityLocations(int count, World world)
    {
        int newLen = _nextComponentIndex + count;
        EnsureCapacity(newLen);

        Span<EntityIDOnly> entitySpan = _entities.AsSpan(_nextComponentIndex, count);

        int componentIndex = _nextComponentIndex;
        ref var recycled = ref world.RecycledEntityIds;
        for (int i = 0; i < entitySpan.Length; i++)
        {
            ref EntityIDOnly archetypeEntity = ref entitySpan[i];

            archetypeEntity = recycled.CanPop() ? recycled.PopUnsafe() : new EntityIDOnly(world.NextEntityID++, 0);

            ref EntityLookup lookup = ref world.EntityTable.UnsafeIndexNoResize(archetypeEntity.ID);

            lookup.Version = archetypeEntity.Version;
            lookup.Location.Archetype = this;
            lookup.Location.Index = componentIndex++;
            lookup.Location.Flags = EntityFlags.None;
        }

        _nextComponentIndex = componentIndex;

        return entitySpan;
    }

    private void Resize(int newLen)
    {
        Array.Resize(ref _entities, newLen);
        var runners = Components;
        for (int i = 1; i < runners.Length; i++)
            runners[i].ResizeBuffer(newLen);
    }

    private void ResizeCreateComponentBuffers()
    {
        int newLen = checked(Math.Max(1, _createComponentBufferEntities.Length) * 2);
        //we only need to resize the EntityIDOnly array when future total entity count is greater than capacity
        Array.Resize(ref _createComponentBufferEntities, newLen);
        var runners = CreateComponentBuffers;
        for (int i = 1; i < runners.Length; i++)
            runners[i].ResizeBuffer(newLen);
    }

    public void EnsureCapacity(int count)
    {
        if (_entities.Length >= count)
        {
            return;
        }

        FastStackArrayPool<EntityIDOnly>.ResizeArrayFromPool(ref _entities, count);
        var runners = Components;
        for (int i = 1; i < runners.Length; i++)
        {
            runners[i].ResizeBuffer(count);
        }
    }

    /// <summary>
    /// This method doesn't modify component storages
    /// </summary>
    internal EntityIDOnly DeleteEntityFromStorage(int index, out int deletedIndex)
    {
        deletedIndex = --_nextComponentIndex;
        Debug.Assert(_nextComponentIndex >= 0);
        return _entities.UnsafeArrayIndex(index) = _entities.UnsafeArrayIndex(_nextComponentIndex);
    }

    internal EntityIDOnly DeleteEntity(int index)
    {
        _nextComponentIndex--;
        Debug.Assert(_nextComponentIndex >= 0);
        //TODO: args
        #region Unroll
        DeleteComponentData args = new(index, _nextComponentIndex);

        ref ComponentStorageBase first = ref MemoryMarshal.GetArrayDataReference(Components);

        switch (Components.Length)
        {
            case 1: goto end;
            case 2: goto len2;
            case 3: goto len3;
            case 4: goto len4;
            case 5: goto len5;
            case 6: goto len6;
            case 7: goto len7;
            case 8: goto len8;
            case 9: goto len9;
            default: goto @long;
        }

    @long:
        var comps = Components;
        for (int i = 9; i < comps.Length; i++)
        {
            comps[i].Delete(args);
        }

    //TODO: figure out the distribution of component counts
    len9:
        Unsafe.Add(ref first, 8).Delete(args);
    len8:
        Unsafe.Add(ref first, 7).Delete(args);
    len7:
        Unsafe.Add(ref first, 6).Delete(args);
    len6:
        Unsafe.Add(ref first, 5).Delete(args);
    len5:
        Unsafe.Add(ref first, 4).Delete(args);
    len4:
        Unsafe.Add(ref first, 3).Delete(args);
    len3:
        Unsafe.Add(ref first, 2).Delete(args);
    len2:
        Unsafe.Add(ref first, 1).Delete(args);
    #endregion

    end:

        return _entities.UnsafeArrayIndex(args.ToIndex) = _entities.UnsafeArrayIndex(args.FromIndex);
    }

    internal void Update(World world)
    {
        if (_nextComponentIndex == 0)
            return;
        var comprunners = Components;
        for (int i = 1; i < comprunners.Length; i++)
            comprunners[i].Run(world, this);
    }

    internal void Update(World world, ComponentID componentID)
    {
        if (_nextComponentIndex == 0)
            return;

        int compIndex = GetComponentIndex(componentID);

        if (compIndex == 0)
            return;

        Components.UnsafeArrayIndex(compIndex).Run(world, this);
    }

    internal void MultiThreadedUpdate(CountdownEvent countdown, World world)
    {
        if (_nextComponentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.MultithreadedRun(countdown, world, this);
    }

    internal void ReleaseArrays()
    {
        _entities = [];
        var comprunners = Components;
        for (int i = 1; i < comprunners.Length; i++)
            comprunners[i].Trim(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex<T>()
    {
        return ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.RawIndex) & GlobalWorldTables.IndexBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(ComponentID component)
    {
        return ComponentTagTable.UnsafeArrayIndex(component.RawIndex) & GlobalWorldTables.IndexBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasTag<T>()
    {
        return (ComponentTagTable.UnsafeArrayIndex(Tag<T>.ID.RawValue) << 7) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasTag(TagID tagID)
    {
        return (ComponentTagTable.UnsafeArrayIndex(tagID.RawValue) << 7) != 0;
    }

    internal Fields Data => new Fields()
    {
        Map = ComponentTagTable,
        Components = Components,
    };

    internal Span<EntityIDOnly> GetEntitySpan()
    {
        Debug.Assert(_nextComponentIndex <= _entities.Length);
#if NETSTANDARD2_1
        return _entities.AsSpan(0, _nextComponentIndex);
#else
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_entities), _nextComponentIndex);
#endif
    }

    internal ref EntityIDOnly GetEntityDataReference() => ref MemoryMarshal.GetArrayDataReference(_entities);

    internal struct Fields
    {
        internal byte[] Map;
        internal ComponentStorageBase[] Components;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T GetComponentDataReference<T>()
        {
            int index = Map.UnsafeArrayIndex(Component<T>.ID.RawIndex);
            return ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(Components.UnsafeArrayIndex(index)).GetComponentStorageDataReference();
        }
    }
}