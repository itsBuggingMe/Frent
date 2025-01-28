using Frent.Buffers;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Extension methods for querying using delegates
/// </summary>
[Variadic("archetype.GetComponentSpan<T>()", "|archetype.GetComponentSpan<T$>(), |")]
[Variadic("ref arg", "|ref arg$, |")]
[Variadic("ref T arg", "|ref T$ arg$, |")]
[Variadic("T>", "|T$, |>")]
public static partial class DelegateQueryExtensions
{
    /// <summary>
    /// Runs a function over every entity in a query
    /// </summary>
    /// <param name="query">The query to execute on</param>
    /// <param name="action">The function to apply to every entity in the <paramref name="query"/></param>
    public static void Run<T>(this Query query, QueryDelegates.Query<T> action)
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponents(
                archetype.ChunkCount,
                archetype.LastChunkComponentCount,
                new Action<T>(action),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct Action<T>(QueryDelegates.Query<T> @delegate) : IAction<T>
    {
        public void Run(ref T arg) => @delegate(ref arg);
    }

    /// <summary>
    /// Runs a function over every entity in a query, while also including the entity itself as an argument in the function
    /// </summary>
    /// <param name="query">The query to execute on</param>
    /// <param name="action">The function to apply to every entity in the <paramref name="query"/></param>
    public static void RunEntity<T>(this Query query, QueryDelegates.QueryEntity<T> action)
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponentsWithEntity(
                archetype.ChunkCount,
                archetype.LastChunkComponentCount,
                new ActionEntity<T>(action),
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct ActionEntity<T>(QueryDelegates.QueryEntity<T> @delegate) : IEntityAction<T>
    {
        public void Run(Entity entity, ref T arg) => @delegate(entity, ref arg);
    }

    /// <summary>
    /// Runs a function over every entity in a query, while also including a uniform as an argument in the function
    /// </summary>
    /// <param name="query">The query to execute on</param>
    /// <param name="action">The function to apply to every entity in the <paramref name="query"/></param>
    public static void RunUniform<TUniform, T>(this Query query, QueryDelegates.QueryUniform<TUniform, T> action)
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponents(
                archetype.ChunkCount,
                archetype.LastChunkComponentCount,
                new ActionUniform<TUniform, T>(uniform, action),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct ActionUniform<TUniform, T>(TUniform uniform, QueryDelegates.QueryUniform<TUniform, T> @delegate) : IAction<T>
    {
        public void Run(ref T arg2) => @delegate(uniform, ref arg2);
    }

    /// <summary>
    /// Runs a function over every entity in a query, while also including the entity itself and a uniform as an arguments in the function
    /// </summary>
    /// <param name="query">The query to execute on</param>
    /// <param name="action">The function to apply to every entity in the <paramref name="query"/></param>
    public static void RunEntityUniform<TUniform, T>(this Query query, QueryDelegates.QueryEntityUniform<TUniform, T> action)
    {
        TUniform uniform = query.World.UniformProvider.GetUniform<TUniform>();
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers<T>.EnumerateComponentsWithEntity(
                archetype.ChunkCount,
                archetype.LastChunkComponentCount,
                new ActionEntityUniform<TUniform, T>(uniform, action),
                archetype.GetEntitySpan(),
                archetype.GetComponentSpan<T>());
        }
    }

    private struct ActionEntityUniform<TUniform, T>(TUniform uniform, QueryDelegates.QueryEntityUniform<TUniform, T> @delegate) : IEntityAction<T>
    {
        public void Run(Entity entity, ref T arg) => @delegate(entity, uniform, ref arg);
    }
}


static partial class DelegateQueryExtensions
{
    /// <summary>
    /// Runs a function over every entity in a query without component data, using only the entity itself
    /// </summary>
    /// <param name="query">The query to execute on</param>
    /// <param name="action">The function to apply to every entity in the <paramref name="query"/></param>
    public static void RunEntity(this Query query, QueryDelegates.QueryEntityOnly action)
    {
        foreach (var archetype in query.AsSpan())
        {
            ChunkHelpers.EnumerateComponentsWithEntity(
                archetype.ChunkCount,
                archetype.LastChunkComponentCount,
                new ActionEntity(action),
                archetype.GetEntitySpan());
        }
    }

    private struct ActionEntity(QueryDelegates.QueryEntityOnly action) : IEntityAction
    {
        public void Run(Entity entity) => action(entity);
    }
}
