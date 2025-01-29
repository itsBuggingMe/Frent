using Frent.Buffers;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Extensions to run chunks actions over a query.
/// </summary>
/// <remarks>Useful for SIMD</remarks>
[Variadic("archetype.GetComponentSpan<T>()", "|archetype.GetComponentSpan<T$>(), |")]
[Variadic("Span<T> arg", "|Span<T$> arg$, |")]
[Variadic(", arg", "|, arg$|")]
[Variadic("T>", "|T$, |>")]
public static partial class ChunkQueryExtensions
{
    public static void RunChunks<TAction, T>(this Query query, TAction action)
        where TAction : struct, IChunkAction<T>, IAction<T>
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateChunks(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                action,
                archetype.GetComponentSpan<T>());
        }
    }

    public static void RunChunksEntity<TAction, T>(this Query query, TAction action)
        where TAction : struct, IEntityChunkAction<T>, IEntityAction<T>
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateChunksWithEntity(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                action,
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    public static void RunChunksUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IUniformChunkAction<TUniform, T>, IUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateChunks(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new ChunkUniformAction<TAction, TUniform, T>(action, uniform),
                new StructQueryExtensions.UniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct ChunkUniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IChunkAction<T>
        where TAction : struct, IUniformChunkAction<TUniform, T>
    {
        public void RunChunk(Span<T> arg) => action.RunChunk(uniform, arg);
    }

    public static void RunChunksEntityUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IEntityUniformChunkAction<TUniform, T>, IEntityUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateChunksWithEntity(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new ChunkEntityUniformAction<TAction, TUniform, T>(action, uniform),
                new StructQueryExtensions.EntityUniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct ChunkEntityUniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IEntityChunkAction<T>
        where TAction : struct, IEntityUniformChunkAction<TUniform, T>
    {
        public void RunChunk(ReadOnlySpan<Entity> entity, Span<T> arg) => action.RunChunk(entity, uniform, arg);
    }
}

static partial class ChunkQueryExtensions
{
    public static void RunChunksEntityOnly<TAction>(this Query query, TAction action)
        where TAction : struct, IEntityChunkAction, IEntityAction
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers.EnumerateChunksWithEntity(
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                action,
                archetype.GetEntitySpan());
        }
    }
}
