using Frent.Buffers;
using Frent.Systems;
using Frent.Variadic.Generator;
using System.Runtime.InteropServices;

namespace Frent;

[Variadic("Query<T>", "Query<|T$, |>")]
[Variadic("Rule.With<T>()", "|Rule.With<T$>(), |")]
[Variadic("                ref Chunk<T> chunk1 = ref chunks1[i];",
    "|                ref Chunk<T$> chunk$ = ref chunks$[i];\n|")]
[Variadic("            var chunks1 = archetype.GetComponentSpan<T>();",
    "|            var chunks$ = archetype.GetComponentSpan<T$>();\n|")]
[Variadic("ref chunk1[j]", "|ref chunk$[j], |")]
[Variadic("QueryHashes<T>", "QueryHashes<|T$, |>")]
[Variadic("QueryEntity<T>", "QueryEntity<|T$, |>")]
[Variadic("<TUniform, T>", "<TUniform, |T$, |>")]
public static partial class WorldDelegateQueryExtensions
{
    public static void Query<T>(this World world, QueryDelegates.Query<T> onEach)
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
                    onEach(ref chunk1[j]);
                }
            }
        }
    }

    public static void Query<T>(this World world, QueryDelegates.QueryEntity<T> onEach)
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
                    onEach(entityChunk[j], ref chunk1[j]);
                }
            }
        }
    }

    public static void QueryEntityUniform<TUniform, T>(this World world, QueryDelegates.QueryEntityUniform<TUniform, T> onEach)
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
                    onEach(entityChunk[j], in uniform, ref chunk1[j]);
                }
            }
        }
    }

    public static void QueryUniform<TUniform, T>(this World world, QueryDelegates.QueryUniform<TUniform, T> onEach)
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
                    onEach(in uniform, ref chunk1[j]);
                }
            }
        }
    }
}

