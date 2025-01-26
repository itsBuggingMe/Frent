using Frent.Buffers;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using System.Runtime.CompilerServices;

namespace Frent.Updating.Runners;

internal abstract class ComponentRunnerBase<TSelf, TComponent> : ComponentStorage<TComponent>, IComponentRunner<TComponent>
    where TSelf : IComponentRunner<TComponent>, new()
{
    public abstract void Run(World world, Archetype b);
    public abstract void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    protected Span<Chunk<TComponent>> Span => _chunks.AsSpan();
    public void Trim(int index) => _chunks[index].Return();
    public void AllocateNextChunk(int chunkSize, int chunkIndex) => Chunk<TComponent>.NextChunk(ref _chunks, chunkSize, chunkIndex);
    public void ResizeChunk(int chunkSize, int chunkIndex) => Array.Resize(ref _chunks[chunkIndex].Buffer, chunkSize);
    public void SetAt(object component, ushort chunkIndex, ushort compIndex) => _chunks[chunkIndex][compIndex] = (TComponent)component;
    public object GetAt(ushort chunkIndex, ushort compIndex) => _chunks[chunkIndex][compIndex]!;
    public void InvokeGenericActionWith(GenericEvent? action, Entity e, ushort chunkIndex, ushort componentIndex) => action?.Invoke(e, ref _chunks[chunkIndex][componentIndex]);
    public ComponentID ComponentID => Component<TComponent>.ID;
    public void PullComponentFrom(IComponentRunner otherRunner, EntityLocation me, EntityLocation other)
    {
        IComponentRunner<TComponent> componentRunner = (IComponentRunner<TComponent>)otherRunner;
        ref var left = ref _chunks[me.ChunkIndex][me.ComponentIndex];
        ref var right = ref componentRunner.AsSpan()[other.ChunkIndex][other.ComponentIndex];
        _chunks[me.ChunkIndex][me.ComponentIndex] = componentRunner.AsSpan()[other.ChunkIndex][other.ComponentIndex];
    }
    public void PullComponentFrom(TrimmableStack storage, EntityLocation me, int other) => _chunks[me.ChunkIndex][me.ComponentIndex] = ((TrimmableStack<TComponent>)storage).StrongBuffer[other];

    public void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom)
    {
        ref var from = ref _chunks[chunkFrom][compFrom];
        _chunks[chunkTo][compTo] = from;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    public TrimmableStack PushComponentToStack(ushort chunkIndex, ushort componentIndex, out int index)
    {
        Component<TComponent>.TrimmableStack.PushStronglyTyped(_chunks[chunkIndex][componentIndex], out index);
        return Component<TComponent>.TrimmableStack;
    }
}

internal abstract class ComponentStorage<TComponent>
{
    internal Chunk<TComponent>[] Chunks => _chunks;
    protected Chunk<TComponent>[] _chunks = [new Chunk<TComponent>(1)];
    public Span<Chunk<TComponent>> AsSpan() => _chunks.AsSpan();
}