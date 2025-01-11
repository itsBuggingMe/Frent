﻿using Frent.Buffers;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using System.Runtime.InteropServices;

namespace Frent;

[Variadic("Query<T>", "Query<|T$, |>")]
[Variadic("Rule.HasComponent(Component<T>.ID)", "|Rule.HasComponent(Component<T$>.ID), |")]
[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("QueryEntity<T>", "QueryEntity<|T$, |>")]
[Variadic("<TUniform, T>", "<TUniform, |T$, |>")]
[Variadic("a.GetComponentSpan<T>()", "|a.GetComponentSpan<T$>(), |")]
[Variadic("ref T arg", "|ref T$ arg$, |")]
[Variadic("ref arg", "|ref arg$, |")]
[Variadic("ChunkHelpers<T>", "ChunkHelpers<|T$, |>")]
public static partial class WorldDelegateQueryExtensions
{
    public static void Query<T>(this World world, QueryDelegates.Query<T> onEach)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpan<DelegateQuery<T>>(a.CurrentWriteChunk, a.LastChunkComponentCount, new(onEach), a.GetComponentSpan<T>());
    }

    internal struct DelegateQuery<T>(QueryDelegates.Query<T> onEach) : IQuery<T>
    {
        public void Run(ref T arg) => onEach(ref arg);
    }

    public static void Query<T>(this World world, QueryDelegates.QueryEntity<T> onEach)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpanEntity<DelegateQueryEntity<T>>(a.CurrentWriteChunk, a.LastChunkComponentCount, new(onEach), a.GetEntitySpan(), a.GetComponentSpan<T>());
    }

    internal struct DelegateQueryEntity<T>(QueryDelegates.QueryEntity<T> onEach) : IQueryEntity<T>
    {
        public void Run(Entity entity, ref T arg) => onEach(entity, ref arg);
    }

    public static void QueryEntityUniform<TUniform, T>(this World world, QueryDelegates.QueryEntityUniform<TUniform, T> onEach)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        DelegateQueryEntityUniform<TUniform, T> uniform = new()
        {
            Uniform = world.UniformProvider.GetUniform<TUniform>(),
            OnEach = onEach,
        };

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpanEntity(a.CurrentWriteChunk, a.LastChunkComponentCount, uniform, a.GetEntitySpan(), a.GetComponentSpan<T>());
    }

    internal struct DelegateQueryEntityUniform<TUniform, T> : IQueryEntity<T>
    {
        internal TUniform Uniform;
        internal QueryDelegates.QueryEntityUniform<TUniform, T> OnEach;
        public void Run(Entity entity, ref T arg) => OnEach(entity, in Uniform, ref arg);
    }

    public static void QueryUniform<TUniform, T>(this World world, QueryDelegates.QueryUniform<TUniform, T> onEach)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.HasComponent(Component<T>.ID));

        DelegateQueryUniform<TUniform, T> uniform = new()
        {
            Uniform = world.UniformProvider.GetUniform<TUniform>(),
            OnEach = onEach,
        };

        foreach (var a in query.AsSpan())
            ChunkHelpers<T>.EnumerateChunkSpan(a.CurrentWriteChunk, a.LastChunkComponentCount, uniform, a.GetComponentSpan<T>());
    }

    internal struct DelegateQueryUniform<TUniform, T> : IQuery<T>
    {
        internal TUniform Uniform;
        internal QueryDelegates.QueryUniform<TUniform, T> OnEach;
        public void Run(ref T arg) => OnEach(in Uniform, ref arg);
    }
}

