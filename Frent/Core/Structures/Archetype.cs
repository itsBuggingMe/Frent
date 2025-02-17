using Frent.Buffers;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Frent.World;

namespace Frent.Core;

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype
{
    internal ArchetypeID ID => _archetypeID;
    internal ImmutableArray<ComponentID> ArchetypeTypeArray => _archetypeID.Types;
    internal ImmutableArray<TagID> ArchetypeTagArray => _archetypeID.Tags;
    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Type.Name))} Tags: {string.Join(", ", ArchetypeTagArray.Select(t => t.Type.Name))}";
    internal int EntityCount => _componentIndex;
    internal Span<T> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GetComponentIndex<T>();
        if (index == 0)
        {
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
        }
        return UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(index)).AsSpan(_componentIndex);
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
        if (_entities.Length == _componentIndex)
            Resize();

        entityLocation.Archetype = this;
        entityLocation.Index = _componentIndex;
        entityLocation.Flags = flags;

        return ref _entities.UnsafeArrayIndex(_componentIndex++);
    }

    internal Span<EntityIDOnly> CreateEntityLocations(int count, World world)
    {
        int newLen = _componentIndex + count;
        EnsureCapacity(newLen);

        Span<EntityIDOnly> entitySpan = _entities.AsSpan(_componentIndex, count);

        int componentIndex = _componentIndex;
        ref var recycled = ref world.RecycledEntityIds;
        for(int i = 0; i < entitySpan.Length; i++)
        {
            ref EntityIDOnly archetypeEntity = ref entitySpan[i];

            archetypeEntity = recycled.CanPop() ? recycled.PopUnsafe() : new EntityIDOnly(world.NextEntityID++, 0);

            ref EntityLookup lookup = ref world.EntityTable.UnsafeIndexNoResize(archetypeEntity.ID);

            lookup.Version = archetypeEntity.Version;
            lookup.Location.Archetype = this;
            lookup.Location.Index = componentIndex++;
            lookup.Location.Flags = EntityFlags.None;
        }

        _componentIndex = componentIndex;

        return entitySpan;
    }

    private void Resize()
    {
        int newLen = checked(_entities.Length * 2);

        Array.Resize(ref _entities, newLen);
        var runners = Components;
        for(int i = 1; i < runners.Length; i++)
        {
            runners[i].ResizeBuffer(newLen);
        }
    }

    public void EnsureCapacity(int count)
    {
        if(_entities.Length >= count)
        {
            return;
        }

        FastStackArrayPool<EntityIDOnly>.ResizeArrayFromPool(ref _entities, count);
        var runners = Components;
        for(int i = 1; i < runners.Length; i++)
        {
            runners[i].ResizeBuffer(count);
        }
    }

    public Archetype FindArchetypeAdjacentRemove(World world, ComponentID component)
    {
        var destination = CreateOrGetExistingArchetype(MemoryHelpers.Remove(ArchetypeTypeArray, component, out var arr), ArchetypeTagArray.AsSpan(), world, arr, ArchetypeTagArray);
        world.CompRemoveLookup.SetArchetype(component.Index, ID, destination);
        return destination;
    }

    public Archetype FindArchetypeAdjacentAdd(World world, ComponentID component)
    {
        var destination = CreateOrGetExistingArchetype(MemoryHelpers.Concat(ArchetypeTypeArray, component, out var res), ArchetypeTagArray.AsSpan(), world, res, ArchetypeTagArray);
        world.CompAddLookup.SetArchetype(component.Index, ID, destination);
        return destination;
    }

    /// <summary>
    /// This method doesn't modify component storages
    /// </summary>
    internal EntityIDOnly DeleteEntityFromStorage(int index)
    {
        _componentIndex--;
        Debug.Assert(_componentIndex >= 0);
        return _entities[index] = _entities[_componentIndex];
    }

    internal EntityIDOnly DeleteEntity(int index)
    {
        _componentIndex--;
        Debug.Assert(_componentIndex >= 0);
        //TODO: args
        #region Unroll
        DeleteComponentData args = new(index, _componentIndex);

        ref IComponentRunner first = ref MemoryMarshal.GetArrayDataReference(Components);

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
        for(int i = 9; i < comps.Length; i++)
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
        
        return _entities.UnsafeArrayIndex(index) = _entities.UnsafeArrayIndex(_componentIndex);
    }

    internal void Update(World world)
    {
        if (_componentIndex == 0)
            return;
        var comprunners = Components;
        for(int i = 1; i < comprunners.Length; i++)
            comprunners[i].Run(world, this);
    }

    internal void Update(World world, ComponentID componentID)
    {
        if (_componentIndex == 0)
            return;

        int compIndex = GetComponentIndex(componentID);

        if (compIndex == 0)
            return;

        Components.UnsafeArrayIndex(compIndex).Run(world, this);
    }

    internal void MultiThreadedUpdate(CountdownEvent countdown, World world)
    {
        if (_componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.MultithreadedRun(countdown, world, this);
    }

    internal void ReleaseArrays()
    {
        _entities = [];
        var comprunners = Components;
        for(int i = 1; i < comprunners.Length; i++)
            comprunners[i].Trim(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex<T>()
    {
        return ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.Index) & GlobalWorldTables.IndexBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(ComponentID component)
    {
        return ComponentTagTable.UnsafeArrayIndex(component.Index) & GlobalWorldTables.IndexBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasTag<T>()
    {
        return (ComponentTagTable.UnsafeArrayIndex(Tag<T>.ID.Index) << 7) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasTag(TagID tagID)
    {
        return (ComponentTagTable.UnsafeArrayIndex(tagID.Index) << 7) != 0;
    }

    internal Span<EntityIDOnly> GetEntitySpan()
    {
        Debug.Assert(_componentIndex <= _entities.Length);
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_entities), _componentIndex);
    }
}