using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Systems;

[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IChunkAction<TArg>
{
    void RunChunk(Span<TArg> arg);
}

[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IEntityChunkAction<TArg>
{
    void RunChunk(ReadOnlySpan<Entity> entity, Span<TArg> arg);
}

public interface IEntityChunkAction
{
    void RunChunk(ReadOnlySpan<Entity> entity);
}

[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IEntityUniformChunkAction<TUniform, TArg>
{
    void RunChunk(ReadOnlySpan<Entity> entity, TUniform uniform, Span<TArg> arg);
}

public interface IEntityUniformChunkAction<TUniform>
{
    void RunChunk(ReadOnlySpan<Entity> entity, TUniform uniform);
}

[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IUniformChunkAction<TUniform, TArg>
{
    void RunChunk(TUniform uniform, Span<TArg> arg);
}