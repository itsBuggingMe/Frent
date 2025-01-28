using Frent.Buffers;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Extension methods for querying in parallel
/// </summary>
[Variadic("archetype.GetComponentSpan<T>()", "|archetype.GetComponentSpan<T$>(), |")]
[Variadic("ref arg", "|ref arg$, |")]
[Variadic("ref T arg", "|ref T$ arg$, |")]
[Variadic("T>", "|T$, |>")]
public static partial class ParallelQueryExtensions
{
    public static void Parallel<TAction, T>(this Query query, TAction action)
        where TAction : struct, IAction<T>
    {
        foreach (var archetype in query.AsSpan())
        {
            MultiThreadHelpers<T>.EnumerateComponents(
                query.World.SharedCountdown,
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                archetype.GetComponentSpan<T>());
        }
    }

    public static void ParallelEntity<TAction, T>(this Query query, TAction action)
        where TAction : struct, IEntityAction<T>
    {
        foreach (var archetype in query.AsSpan())
        {
            MultiThreadHelpers<T>.EnumerateComponentsWithEntity(
                query.World.SharedCountdown,
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    public static void ParallelUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            MultiThreadHelpers<T>.EnumerateComponents(
                query.World.SharedCountdown,

                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new UniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetComponentSpan<T>());
        }
    }

    internal struct UniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IAction<T>
        where TAction : struct, IUniformAction<TUniform, T>
    {
        public void Run(ref T arg) => action.Run(uniform, ref arg);
    }

    public static void ParallelEntityUniform<TAction, TUniform, T>(this Query query, TAction action)
        where TAction : struct, IEntityUniformAction<TUniform, T>
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            MultiThreadHelpers<T>.EnumerateComponentsWithEntity(
                query.World.SharedCountdown,
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                new EntityUniformAction<TAction, TUniform, T>(action, uniform),
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    internal struct EntityUniformAction<TAction, TUniform, T>(TAction action, TUniform uniform) : IEntityAction<T>
        where TAction : struct, IEntityUniformAction<TUniform, T>
    {
        public void Run(Entity entity, ref T arg) => action.Run(entity, uniform, ref arg);
    }
}

static partial class ParallelQueryExtensions
{
    public static void ParallelEntity<TAction>(this Query query, TAction action)
        where TAction : struct, IEntityAction
    {
        foreach (var archetype in query.AsSpan())
        {
            MultiThreadHelpers.EnumerateComponentsWithEntity(
                query.World.SharedCountdown,
                archetype.CurrentWriteChunk,
                archetype.LastChunkComponentCount,
                action,
                archetype.GetEntitySpan());
        }
    }
}