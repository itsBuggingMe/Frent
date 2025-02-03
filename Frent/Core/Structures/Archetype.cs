using Frent.Buffers;
using Frent.Core.Structures;
using Frent.Updating;
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
        get
        {
            int sum = 0;
            for (int i = 0; i < _chunkIndex; i++)
            {
                sum += _entities[i].Length;
            }
            return sum + _componentIndex;
        }
    }

    internal int LastChunkComponentCount => _componentIndex;
    internal ushort ChunkCount => _chunkIndex;
    internal ushort CurrentWriteChunk => _chunkIndex;

    internal Span<Chunk<T>> GetComponentSpan<T>()
    {
        var components = Components;
        int index = GlobalWorldTables.ComponentIndex(ID, Component<T>.ID);
        if (index > components.Length)
        {
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
            return default;
        }
        return ((IComponentRunner<T>)components[index]).AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Entity CreateEntityLocation(EntityFlags flags, out EntityLocation entityLocation)
    {
        if (_entities.UnsafeArrayIndex(_chunkIndex).Length == _componentIndex)
            CreateChunks();

        entityLocation = new EntityLocation(ID, _chunkIndex, (ushort)_componentIndex, flags);
        return ref _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex++];
    }

    private void CreateChunks()
    {
        _chunkSize = Math.Min(MaxChunkSize, _chunkSize << 2);

        if (_chunkSize >= 16)
        {//try to keep chunk sizes >= 16
            _chunkIndex++;
            _componentIndex = 0;

            Chunk<Entity>.NextChunk(ref _entities, _chunkSize, _chunkIndex);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk(_chunkSize, _chunkIndex);
        }
        else
        {//resize existing array
            Array.Resize(ref _entities[0].Buffer, _chunkSize);
            foreach (var comprunner in Components)
                comprunner.ResizeChunk(_chunkSize, 0);
        }
    }

    public void EnsureCapacity(int count)
    {
        _chunkSize = Math.Min(MaxChunkSize, MemoryHelpers.RoundUpToNextMultipleOf16(count));

        while (count > 0)
        {
            Chunk<Entity>.NextChunk(ref _entities, _chunkSize, _chunkIndex);
            foreach (var comprunner in Components)
                comprunner.AllocateNextChunk(_chunkSize, _chunkIndex);

            count -= _chunkSize;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Entity DeleteEntity(ushort chunk, ushort comp)
    {
        if (unchecked(--_componentIndex == -1))
        {
            return DeleteEntityAndShrink(chunk, comp);
        }

        #region Unroll
        ref IComponentRunner first = ref MemoryMarshal.GetArrayDataReference(Components);
        DeleteComponentData args = new(chunk, comp, _chunkIndex, (ushort)_componentIndex);

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

        return _entities.UnsafeArrayIndex(chunk).Buffer.UnsafeArrayIndex(comp) = _entities.UnsafeArrayIndex(_chunkIndex).Buffer.UnsafeArrayIndex(_componentIndex);
    }

    private Entity DeleteEntityAndShrink(ushort chunk, ushort comp)
    {
        _chunkIndex--;
        _componentIndex = _entities[_chunkIndex].Length - 1;

        DeleteComponentData arg = new DeleteComponentData(chunk, comp, _chunkIndex, (ushort)_componentIndex);
        foreach (var comprunner in Components)
            comprunner.Delete(arg);

        var e = _entities.UnsafeArrayIndex(chunk)[comp] = _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex];

        int index = _chunkIndex + 1;
        _entities[index].Return();
        foreach (var comprunner in Components)
            comprunner.Trim(index);
        return e;
    }

    internal void Update(World world)
    {
        if ((_chunkIndex | _componentIndex) == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(world, this);
    }

    internal void Update(World world, ComponentID componentID)
    {
        //avoid the second branch   
        if ((_chunkIndex | _componentIndex) == 0)
            return;

        int compIndex = GlobalWorldTables.ComponentIndex(ID, componentID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            return;

        Components.UnsafeArrayIndex(compIndex).Run(world, this);
    }

    internal void MultiThreadedUpdate(CountdownEvent countdown, World world)
    {
        if ((_chunkIndex | _componentIndex) == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.MultithreadedRun(countdown, world, this);
    }

    internal void ReleaseArrays()
    {
        for (int i = 0; i <= _chunkIndex; i++)
        {
            _entities[i].Return();
            foreach (var comprunner in Components)
                comprunner.Trim(i);
        }
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();
}