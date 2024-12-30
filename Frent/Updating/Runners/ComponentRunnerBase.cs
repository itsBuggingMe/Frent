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
    internal abstract void Run(Archetype b);
    protected Span<Chunk<TComponent>> Span => _chunks.AsSpan();

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
