using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Systems;

/// <summary>
/// Extensions to execute behavior on queries.
/// </summary>
[Variadic(nameof(QueryIterationExtensions))]
public static partial class QueryIterationExtensions
{
    /// <summary>
    /// Executes a delegate for every entity in a query, using the specified component types.
    /// </summary>
    /// <param name="query">The query to iterate over.</param>
    /// <param name="action">The behavior to execute on every component set.</param>
    /// <variadic />
    public static void Delegate<T>(this Query query, QueryDelegates.Query<T> action) => Inline<Bridge<T>, T>(query, new(action));

    private struct Bridge<T>(QueryDelegates.Query<T> q) : IAction<T>
    {
        public void Run(ref T arg) => q.Invoke(ref arg);
    }

    /// <summary>
    /// Executes a inlinable struct instance method for every entity in a query, using the specified component types.
    /// </summary>
    /// <param name="query">The query to iterate over.</param>
    /// <param name="action">The struct behavior to execute on every component set.</param>
    /// <variadic />
    public static void Inline<TAction, T>(this Query query, TAction action)
        where TAction : IAction<T>
    {
        query.AssertHasSparseComponent<T>();

        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(query.World.WorldSparseSetTable);

        if(!query.HasSparseExclusions)
        {
            ref T sparseFirst = ref IRunner.InitSparse<T>(ref first, out Span<int> sparseArgArray);

            foreach (var archetype in query.AsSpan())
            {
                //use ref instead of span to avoid extra locals
                ref T c1 = ref Component<T>.IsSparseComponent ?
                    ref Unsafe.NullRef<T>() :
                    ref archetype.GetComponentDataReference<T>();

                ref EntityIDOnly entity = ref archetype.GetEntityDataReference();

                for (nint i = archetype.EntityCount - 1; i >= 0; i--)
                {
                    if (Component<T>.IsSparseComponent)
                    {
                        int id = entity.ID;
                        if (!((uint)id < (uint)sparseArgArray.Length)) continue;
                        int index = sparseArgArray[id];
                        if (index < 0) continue;
                        c1 = ref Unsafe.Add(ref sparseFirst, index);
                    }

                    action.Run(ref c1);

                    entity = ref Unsafe.Add(ref entity, 1);
                    if (!Component<T>.IsSparseComponent) c1 = ref Unsafe.Add(ref c1, 1);
                }
            }
        }
        else
        {// do extra work to exclude sparse components
            InlineSparseExcludeImpl<TAction, T>(ref first, query, action);
        }
    }

    internal static void InlineSparseExcludeImpl<TAction, T>(ref ComponentSparseSetBase first, Query query, TAction action)
        where TAction : IAction<T>
    {
        Bitset excludeBits = query.ExcludeMask;
        Span<Bitset> worldBitsets = query.World.SparseComponentTable;

        ref T sparseFirst = ref IRunner.InitSparse<T>(ref first, out Span<int> sparseArgArray);

        foreach (var archetype in query.AsSpan())
        {
            //use ref instead of span to avoid extra locals
            scoped ref T c1 = ref Component<T>.IsSparseComponent ?
                ref Unsafe.NullRef<T>() :
                ref archetype.GetComponentDataReference<T>();

            ref EntityIDOnly entity = ref archetype.GetEntityDataReference();

            for (nint i = archetype.EntityCount - 1; i >= 0; i--)
            {
                int id = entity.ID;
                if (Component<T>.IsSparseComponent)
                {
                    if (!((uint)id < (uint)sparseArgArray.Length)) continue;
                    int index = sparseArgArray[id];
                    if (index < 0) continue;
                    c1 = ref Unsafe.Add(ref sparseFirst, index);
                }

                // exclude
                if ((uint)id < (uint)worldBitsets.Length && Bitset.AndAndThenAnySet(ref excludeBits, ref worldBitsets[id]))
                {
                    continue;
                }

                action.Run(ref c1);

                entity = ref Unsafe.Add(ref entity, 1);
                if (!Component<T>.IsSparseComponent) c1 = ref Unsafe.Add(ref c1, 1);
            }
        }
    }
}
