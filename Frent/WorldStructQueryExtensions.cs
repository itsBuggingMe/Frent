using Frent.Buffers;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using System.Runtime.InteropServices;

namespace Frent;

[Variadic("<TQuery, T>", "<TQuery, |T$, |>")]
[Variadic("<TQuery, T>", "<TQuery, |T$, |>")]
[Variadic("Rule.HasComponent(Component<T>.ID)", "|Rule.HasComponent(Component<T$>.ID), |")]
[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("QueryEntity<T>", "QueryEntity<|T$, |>")]
[Variadic("Query<T>", "Query<|T$, |>")]
[Variadic("<TQuery, TUniform, T>", "<TQuery, TUniform, |T$, |>")]
[Variadic("Uniform<TUniform, T>", "Uniform<TUniform, |T$, |>")]
[Variadic("ref T arg", "|ref T$ arg$, |")]
[Variadic("ref arg", "|ref arg$, |")]
[Variadic("ChunkHelpers<T>", "ChunkHelpers<|T$, |>")]
[Variadic("a.GetComponentSpan<T>()", "|a.GetComponentSpan<T$>(), |")]
public static partial class WorldStructQueryExtensions
{

#pragma warning disable CS1712

    /// <summary>
    /// Runs a method on every entity that has the specified component types
    /// </summary>
    /// <typeparam name="TQuery">The type of the function to be ran on every entity</typeparam>
    /// <param name="world">The world to query on</param>
    /// <param name="onEach">The action to be applied to every entity</param>
    public static void InlineQuery<TQuery, T>(this World world, TQuery onEach)
        where TQuery : IQuery<T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        world.EnterDisallowState();

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpan(a.CurrentWriteChunk, a.LastChunkComponentCount, onEach, a.GetComponentSpan<T>());

        world.ExitDisallowState();
    }

    /// <summary>
    /// Runs a method on every entity that has the specified component types. Also includes the "self" <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="TQuery">The type of the function to be ran on every entity</typeparam>
    /// <param name="world">The world to query on</param>
    /// <param name="onEach">The action to be applied to every entity</param>
    public static void InlineQueryEntity<TQuery, T>(this World world, TQuery onEach)
        where TQuery : IQueryEntity<T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        world.EnterDisallowState();

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpanEntity(a.CurrentWriteChunk, a.LastChunkComponentCount, onEach, a.GetEntitySpan(), a.GetComponentSpan<T>());

        world.ExitDisallowState();
    }

    /// <summary>
    /// Runs a method on every entity that has the specified component types. Also includes the "self" <see cref="Entity"/> and a uniform value.
    /// </summary>
    /// <typeparam name="TQuery">The type of the function to be ran on every entity</typeparam>
    /// <typeparam name="TUniform">The type of uniform to be supplied into the update function</typeparam>
    /// <param name="world">The world to query on</param>
    /// <param name="onEach">The action to be applied to every entity</param>
    public static void InlineQueryEntityUniform<TQuery, TUniform, T>(this World world, TQuery onEach)
        where TQuery : IQueryEntityUniform<TUniform, T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        EntityUniformActionBridge<TQuery, TUniform, T> finalOnEach = new()
        {
            Uniform = world.UniformProvider.GetUniform<TUniform>(),
            Query = onEach,
        };

        world.EnterDisallowState();

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpanEntity(a.CurrentWriteChunk, a.LastChunkComponentCount, finalOnEach, a.GetEntitySpan(), a.GetComponentSpan<T>());

        world.ExitDisallowState();
    }

    internal struct EntityUniformActionBridge<TQuery, TUniform, T> : IQueryEntity<T>
        where TQuery : IQueryEntityUniform<TUniform, T>
    {
        internal TUniform Uniform;
        internal TQuery Query;
        public void Run(Entity entity, ref T arg) => Query.Run(entity, in Uniform, ref arg);
    }

    /// <summary>
    /// Runs a method on every entity that has the specified component types. Also includes a uniform value.
    /// </summary>
    /// <typeparam name="TQuery">The type of the function to be ran on every entity</typeparam>
    /// <typeparam name="TUniform">The type of uniform to be supplied into the update function</typeparam>
    /// <param name="world">The world to query on</param>
    /// <param name="onEach">The action to be applied to every entity</param>
    public static void InlineQueryUniform<TQuery, TUniform, T>(this World world, TQuery onEach)
        where TQuery : IQueryUniform<TUniform, T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        UniformActionBridge<TQuery, TUniform, T> finalOnEach = new()
        {
            Uniform = world.UniformProvider.GetUniform<TUniform>(),
            Query = onEach,
        };

        world.EnterDisallowState();

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpan(a.CurrentWriteChunk, a.LastChunkComponentCount, finalOnEach, a.GetComponentSpan<T>());

        world.ExitDisallowState();
    }

    internal struct UniformActionBridge<TQuery, TUniform, T> : IQuery<T>
        where TQuery : IQueryUniform<TUniform, T>
    {
        internal TUniform Uniform;
        internal TQuery Query;

        public void Run(ref T arg) => Query.Run(in Uniform, ref arg);
    }
}

