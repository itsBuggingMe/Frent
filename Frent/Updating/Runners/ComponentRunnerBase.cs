using Frent.Buffers;
using Frent.Core;
using System.Runtime.CompilerServices;

namespace Frent.Updating.Runners;

public abstract class ComponentRunnerBase<TSelf, TComponent> : IComponentRunner<TComponent>
    where TSelf : IComponentRunner<TComponent>, new()
{
    private Chunk<TComponent>[] _chunks = [new Chunk<TComponent>(1)];
    public Span<Chunk<TComponent>> AsSpan() => _chunks.AsSpan();
    public IComponentRunner Clone() => new TSelf();
    public IComponentRunner<TComponent> CloneStronglyTyped() => new TSelf();
    public abstract void Run(Archetype b);
    protected Span<Chunk<TComponent>> Span => _chunks.AsSpan();
    public void AllocateNextChunk() => Chunk<TComponent>.NextChunk(ref _chunks);
    internal void Clear()
    {
        if(_chunks.Length == 1)
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
        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }


    //    Chunk<T>.NextChunk(ref _cBuffer1);

    //public void DeleteComponent(ref readonly DeleteComponentData delete)
    //{
    //    var chunks = _chunks;
    //    ref var componentToBeMovedDown = ref chunks[delete.TopChunkIndex][delete.TopEntityIndex];
    //    chunks[delete.ChunkIndex][delete.EntityIndex] = componentToBeMovedDown;
    //
    //    if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
    //        componentToBeMovedDown = default!;
    //}
}
