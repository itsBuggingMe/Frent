using Frent.Buffers;

namespace Frent.Updating;

public interface IComponentRunner
{
    internal IComponentRunner Clone();
}

public interface IComponentRunner<T> : IComponentRunner
{
    internal Span<Chunk<T>> AsSpan();
    internal IComponentRunner<T> CloneStronglyTyped();
}