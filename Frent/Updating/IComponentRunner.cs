using Frent.Buffers;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;

namespace Frent.Updating;

internal interface IComponentRunner
{
    internal void Run(World world, Archetype b);
    internal void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    internal void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom);
    internal void Trim(int chunkIndex);
    internal void AllocateNextChunk(int size, int chunkIndex);
    internal void ResizeChunk(int size, int chunkIndex);
    internal void PullComponentFrom(IComponentRunner otherRunner, EntityLocation me, EntityLocation other);
    internal void PullComponentFrom(TrimmableStack storage, EntityLocation me, int other);
    internal void InvokeGenericActionWith(GenericEvent? action, Entity entity, ushort chunkIndex, ushort componentIndex);
    internal TrimmableStack PushComponentToStack(ushort chunkIndex, ushort componentIndex, out int index);
    internal void SetAt(object component, ushort chunkIndex, ushort compIndex);
    internal object GetAt(ushort chunkIndex, ushort compIndex);
    internal ComponentID ComponentID { get; }
}

internal interface IComponentRunner<T> : IComponentRunner
{
    internal Span<Chunk<T>> AsSpan();
}