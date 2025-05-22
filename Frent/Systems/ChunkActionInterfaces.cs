using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Systems;

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
/// <variadic />
[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IChunkAction<TArg>
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(Span<TArg> arg);
}

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
/// <variadic />
[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IEntityChunkAction<TArg>
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(ReadOnlySpan<Entity> entity, Span<TArg> arg);
}

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
public interface IEntityChunkAction
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(ReadOnlySpan<Entity> entity);
}

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IEntityUniformChunkAction<TUniform, TArg>
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(ReadOnlySpan<Entity> entity, TUniform uniform, Span<TArg> arg);
}

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
public interface IEntityUniformChunkAction<TUniform>
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(ReadOnlySpan<Entity> entity, TUniform uniform);
}

/// <summary>
/// An arbitary function that operates over a range of components
/// </summary>
/// <remarks>Used for SIMD</remarks>
[Variadic(SpanArgFrom, SpanArgPattern)]
[Variadic(TArgFrom, TArgPattern)]
public interface IUniformChunkAction<TUniform, TArg>
{
    /// <summary>
    /// Executes the function
    /// </summary>
    void RunChunk(TUniform uniform, Span<TArg> arg);
}