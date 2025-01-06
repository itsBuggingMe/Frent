using Frent.Buffers;
using Frent.Core;
using System.Runtime.CompilerServices;

namespace Frent.Updating.Runners;

internal abstract class ComponentRunnerBase<TSelf, TComponent> : ComponentStorage<TComponent>, IComponentRunner<TComponent>
    where TSelf : IComponentRunner<TComponent>, new()
{
    public abstract void Run(Archetype b);
    protected Span<Chunk<TComponent>> Span => _chunks.AsSpan();
    public void AllocateNextChunk(int chunkSize, int chunkIndex) => Chunk<TComponent>.NextChunk(ref _chunks, chunkSize, chunkIndex);
    public void SetAt(object component, ushort chunkIndex, ushort compIndex) => _chunks[chunkIndex][compIndex] = (TComponent)component;
    public object GetAt(ushort chunkIndex, ushort compIndex) => _chunks[chunkIndex][compIndex]!;
    public ComponentID ComponentID => Component<TComponent>.ID;
    public void PullComponentFrom(IComponentRunner otherRunner, EntityLocation me, EntityLocation other)
    {
        IComponentRunner<TComponent> componentRunner = (IComponentRunner<TComponent>)otherRunner;
        ref var left = ref _chunks[me.ChunkIndex][me.ComponentIndex];
        ref var right = ref componentRunner.AsSpan()[other.ChunkIndex][other.ComponentIndex];
        _chunks[me.ChunkIndex][me.ComponentIndex] = componentRunner.AsSpan()[other.ChunkIndex][other.ComponentIndex];
    }

    internal void Clear()
    {
        if (_chunks.Length == 1)
        {
            _chunks[0][0] = default!;
        }
        else
        {
            _chunks = [new Chunk<TComponent>(1)];
        }
    }

    public void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom)
    {
        ref var from = ref _chunks[chunkFrom][compFrom];
        _chunks[chunkTo][compTo] = from;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }
}

internal abstract class ComponentStorage<TComponent>
{
    protected Chunk<TComponent>[] _chunks = [new Chunk<TComponent>(1)];
    public Span<Chunk<TComponent>> AsSpan() => _chunks.AsSpan();
}