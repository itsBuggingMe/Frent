using Frent.Buffers;
using Frent.Core;

namespace Frent.Updating;

public interface IComponentRunner
{
    internal IComponentRunner Clone();
    internal void Run(Archetype archetype);
    internal void Delete(ushort chunkTo, ushort compTo, ushort chunkFrom, ushort compFrom);
    internal void AllocateNextChunk();
}

public interface IComponentRunner<T> : IComponentRunner
{
    internal Span<Chunk<T>> AsSpan();
    internal IComponentRunner<T> CloneStronglyTyped();
}