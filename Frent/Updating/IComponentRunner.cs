using Frent.Buffers;
using Frent.Core;

namespace Frent.Updating;

public interface IComponentRunner
{
    internal IComponentRunner Clone();
    internal void Run(Archetype archetype);
    internal void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom);
    internal void AllocateNextChunk(int size);
    internal void PullComponentFrom(IComponentRunner otherRunner, ref readonly EntityLocation me, ref readonly EntityLocation other);
    internal void SetAt(object component, ushort chunkIndex, ushort compIndex);
    internal object GetAt(ushort chunkIndex, ushort compIndex);
    internal int ComponentID { get; }
}

public interface IComponentRunner<T> : IComponentRunner
{
    internal Span<Chunk<T>> AsSpan();
    internal IComponentRunner<T> CloneStronglyTyped();
}