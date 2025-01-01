using Frent.Buffers;
using Frent.Systems;
using Frent.Variadic.Generator;
using System.Runtime.InteropServices;

namespace Frent;

[Variadic("<TQuery, T>", "<TQuery, |T$, |>")]
[Variadic("<TQuery, T>", "<TQuery, |T$, |>")]
[Variadic("Rule.With<T>()", "|Rule.With<T$>(), |")]
[Variadic("                ref Chunk<T> chunk1 = ref chunks1[i];",
    "|                ref Chunk<T$> chunk$ = ref chunks$[i];\n|")]
[Variadic("            var chunks1 = archetype.GetComponentSpan<T>();",
    "|            var chunks$ = archetype.GetComponentSpan<T$>();\n|")]
[Variadic("ref chunk1[j]", "|ref chunk$[j], |")]
[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("QueryEntity<T>", "QueryEntity<|T$, |>")]
[Variadic("Query<T>", "Query<|T$, |>")]
[Variadic("<TQuery, TUniform, T>", "<TQuery, TUniform, |T$, |>")]
[Variadic("Uniform<TUniform, T>", "Uniform<TUniform, |T$, |>")]
public static partial class WorldStructQueryExtensions
{
    //TODO: refactor
    //this code duplication is shit
    public static void InlineQuery<TQuery, T>(this World world, TQuery onEach)
        where TQuery : IQuery<T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.With<T>());

        foreach (var archetype in query)
        {
            var chunks1 = archetype.GetComponentSpan<T>();

            for (int i = 0; i < chunks1.Length; i++)
            {
                ref Chunk<T> chunk1 = ref chunks1[i];

                for (int j = 0; j < chunk1.Length; j++)
                {
                    onEach.Run(ref chunk1[j]);
                }
            }
        }
    }

    public static void InlineQueryEntity<TQuery, T>(this World world, TQuery onEach)
        where TQuery : IQueryEntity<T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.With<T>());

        //TODO: Elide bounds checking
        foreach (var archetype in query)
        {
            var entityChunks = archetype.GetEntitySpan();
            var chunks1 = archetype.GetComponentSpan<T>();

            for (int i = 0; i < chunks1.Length; i++)
            {
                ref var entityChunk = ref entityChunks[i];
                ref Chunk<T> chunk1 = ref chunks1[i];

                for (int j = 0; j < chunk1.Length; j++)
                {
                    onEach.Run(entityChunk[j], ref chunk1[j]);
                }
            }
        }
    }

    public static void InlineQueryEntityUniform<TQuery, TUniform, T>(this World world, TQuery onEach)
        where TQuery : IQueryEntityUniform<TUniform, T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.With<T>());

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        //TODO: Elide bounds checking
        foreach (var archetype in query)
        {
            var entityChunks = archetype.GetEntitySpan();
            var chunks1 = archetype.GetComponentSpan<T>();

            for (int i = 0; i < chunks1.Length; i++)
            {
                ref var entityChunk = ref entityChunks[i];
                ref Chunk<T> chunk1 = ref chunks1[i];

                for (int j = 0; j < chunk1.Length; j++)
                {
                    onEach.Run(entityChunk[j], in uniform, ref chunk1[j]);
                }
            }
        }
    }

    public static void InlineQueryUniform<TQuery, TUniform, T>(this World world, TQuery onEach)
        where TQuery : IQueryUniform<TUniform, T>
    {
        ArgumentNullException.ThrowIfNull(onEach, nameof(onEach));

        Query query = CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, QueryHashes<T>.Hash, out _) ??=
            world.CreateQuery(Rule.With<T>());

        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        //TODO: Elide bounds checking
        foreach (var archetype in query)
        {
            var chunks1 = archetype.GetComponentSpan<T>();

            for (int i = 0; i < chunks1.Length; i++)
            {
                ref Chunk<T> chunk1 = ref chunks1[i];

                for (int j = 0; j < chunk1.Length; j++)
                {
                    onEach.Run(in uniform, ref chunk1[j]);
                }
            }
        }
    }
}

