using Frent.Buffers;
using Frent.Core;

namespace Frent.Updating;

public interface IComponentRunner
{
    internal void Run(Archetype archetype);
    internal void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom);
    internal void AllocateNextChunk(int size, int chunkIndex);
    internal void PullComponentFrom(IComponentRunner otherRunner, EntityLocation me, EntityLocation other);
    internal void SetAt(object component, ushort chunkIndex, ushort compIndex);
    internal object GetAt(ushort chunkIndex, ushort compIndex);
    internal ComponentID ComponentID { get; }
}

public interface IComponentRunner<T> : IComponentRunner
{
    internal Span<Chunk<T>> AsSpan();
}