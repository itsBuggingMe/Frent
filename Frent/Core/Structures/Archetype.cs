using Frent.Buffers;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype
{
    internal static int MaxChunkSize => MemoryHelpers.MaxArchetypeChunkSize;
    internal ArchetypeID ID => _archetypeID;
    internal ImmutableArray<ComponentID> ArchetypeTypeArray => _archetypeID.Types;
    internal ImmutableArray<TagID> ArchetypeTagArray => _archetypeID.Tags;

    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Type.Name))} Tags: {string.Join(", ", ArchetypeTagArray.Select(t => t.Type.Name))}";

    internal int EntityCount
    {
        get => _componentIndex;
    }

    internal Span<T> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GlobalWorldTables.ComponentIndex(ID, Component<T>.ID);
        if (index > components.Length)
        {
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
            return default;
        }
        return UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components[index]).AsSpan();
    }

    internal ref EntityIDOnly CreateEntityLocation(EntityFlags flags, out EntityLocation entityLocation)
    {
        int elen = _entities.Length;
        int comlen = _componentIndex;
        if (elen == comlen)
            Resize();

        entityLocation = new EntityLocation(ID, comlen, flags);
        return ref _entities.UnsafeArrayIndex(_componentIndex++);
    }

    private void Resize()
    {
        int newLen = checked(_entities.Length * 2);

        Array.Resize(ref _entities, newLen);
        foreach (var comprunner in Components)
            comprunner.ResizeBuffer(newLen);
    }

    public void EnsureCapacity(int count)
    {
        int newLen = checked(MemoryHelpers.RoundUpToNextMultipleOf16(count));

        if(_entities.Length >= newLen)
        {
            return;
        }

        FastStackArrayPool<EntityIDOnly>.ResizeArrayFromPool(ref _entities, newLen);
        foreach (var comprunner in Components)
            comprunner.ResizeBuffer(newLen);
    }

    /// <summary>
    /// This method doesn't modify component storages
    /// </summary>
    internal EntityIDOnly DeleteEntityFromStorage(int index)
    {
        _componentIndex--;
        Debug.Assert(_componentIndex >= 0);
        return _entities.UnsafeArrayIndex(index) = _entities.UnsafeArrayIndex(_componentIndex);
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
            case 1: goto len1;
            case 2: goto len2;
            case 3: goto len3;
            case 4: goto len4;
            case 5: goto len5;
            case 6: goto len6;
            case 7: goto len7;
            case 8: goto len8;
            case 9: goto len9;
            case 10: goto len10;
            case 11: goto len11;
            case 12: goto len12;
            case 13: goto len13;
            case 14: goto len14;
            case 15: goto len15;
            case 16: goto len16;
            default: goto end;
        }

    len16:
        Unsafe.Add(ref first, 15).Delete(args);
    len15:
        Unsafe.Add(ref first, 14).Delete(args);
    len14:
        Unsafe.Add(ref first, 13).Delete(args);
    len13:
        Unsafe.Add(ref first, 12).Delete(args);
    len12:
        Unsafe.Add(ref first, 11).Delete(args);
    len11:
        Unsafe.Add(ref first, 10).Delete(args);
    len10:
        Unsafe.Add(ref first, 9).Delete(args);
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
    len1:
        Unsafe.Add(ref first, 0).Delete(args);
        #endregion

    end:

        return _entities.UnsafeArrayIndex(index) = _entities.UnsafeArrayIndex(_componentIndex);
    }

    internal void Update(World world)
    {
        if (_componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(world, this);
    }

    internal void Update(World world, ComponentID componentID)
    {
        if (_componentIndex == 0)
            return;

        int compIndex = GlobalWorldTables.ComponentIndex(ID, componentID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
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
        foreach (var comprunner in Components)
            comprunner.Trim(0);
    }

    internal Span<EntityIDOnly> GetEntitySpan() => _entities.AsSpan(0, _componentIndex);
}