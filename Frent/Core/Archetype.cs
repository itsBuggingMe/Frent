using Frent.Buffers;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Core;

[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
internal partial class Archetype
{
    internal static int MaxChunkSize => MemoryHelpers.MaxArchetypeChunkSize;
    internal ArchetypeID ID => _archetypeID;
    internal ImmutableArray<Type> ArchetypeTypeArray => _archetypeID.Types;
    internal ImmutableArray<Type> ArchetypeTagArray => _archetypeID.Tags;

    internal string DebuggerDisplayString => $"Archetype Count: {EntityCount} Types: {string.Join(", ", ArchetypeTypeArray.Select(t => t.Name))} Tags: {string.Join(", ", ArchetypeTagArray.Select(t => t.Name))}";

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
    internal ref Entity CreateEntityLocation(out EntityLocation entityLocation)
    {
        if (_entities.UnsafeArrayIndex(_chunkIndex).Length == _componentIndex)
            CreateChunks();

        entityLocation = new EntityLocation(ID, _chunkIndex, (ushort)_componentIndex);
        return ref _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex++];
    }

    private void CreateChunks()
    {
        _chunkSize = Math.Min(MaxChunkSize, _chunkSize << 2);

        if(_chunkSize >= 16)
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


    internal Entity DeleteEntity(ushort chunk, ushort comp)
    {
        if (unchecked(--_componentIndex == -1))
        {
            _chunkIndex--;
            _componentIndex = _entities[_chunkIndex].Length - 1;

            foreach (var comprunner in Components)
                comprunner.Delete(chunk, comp, _chunkIndex, (ushort)_componentIndex);

            var e =  _entities.UnsafeArrayIndex(chunk)[comp] = _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex];

            int index = _chunkIndex + 1;
            _entities[index].Return();
            foreach (var comprunner in Components)
                comprunner.Trim(index);

            return e;
        }


        foreach (var comprunner in Components)
            comprunner.Delete(chunk, comp, _chunkIndex, (ushort)_componentIndex);

        return _entities.UnsafeArrayIndex(chunk)[comp] = _entities.UnsafeArrayIndex(_chunkIndex)[_componentIndex];
    }

    internal void Update(World world)
    {
        if (_chunkIndex == 0 && _componentIndex == 0)
            return;
        foreach (var comprunner in Components)
            comprunner.Run(world, this);
    }

    internal void Update(World world, ComponentID componentID)
    {
        if (_chunkIndex == 0 && _componentIndex == 0)
            return;
        
        int compIndex = GlobalWorldTables.ComponentIndex(ID, componentID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            return;
        
        Components[compIndex].Run(world, this);
    }

    internal void MultiThreadedUpdate(Config config)
    {
        throw new NotImplementedException();
    }

    internal void ReleaseArrays()
    {
        for(int i = 0; i <= _chunkIndex; i++)
        {
            _entities[i].Return();
            foreach (var comprunner in Components)
                comprunner.Trim(i);
        }
    }

    internal Span<Chunk<Entity>> GetEntitySpan() => _entities.AsSpan();
}