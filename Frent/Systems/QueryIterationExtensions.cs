using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
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

    try
    {
        query.World.EnterDisallowState();

        if (!query.HasSparseRules)
        {
            foreach (var archetype in query.AsSpan())
            {
                //use ref instead of span to avoid extra locals
                ref T c1 = ref archetype.GetComponentDataReference<T>();

                for (int i = 0; i < archetype.EntityCount; i++)
                {
                    action.Run(ref c1);

                    c1 = ref Unsafe.Add(ref c1, 1);
                }
            }
        }
        else
        {// do extra work for sparse includes and excludes
            ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(query.World.WorldSparseSetTable);
            InlineSparseRuleImpl<TAction, T>(ref first, query, action);
        }
    }
    finally
    {
        query.World.ExitDisallowState(null);
    }
    }

    internal static void InlineSparseRuleImpl<TAction, T>(ref ComponentSparseSetBase first, Query query, TAction action)
        where TAction : IAction<T>
    {
        Bitset includeBits = query.IncludeMask;
        Bitset excludeBits = query.ExcludeMask;

        ref T sparseFirst = ref IRunner.InitSparse<T>(ref first, out Span<int> sparseArgArray);

        foreach (var archetype in query.AsSpan())
        {
            Span<Bitset> bitset = archetype.SparseBitsetSpan();

            //use ref instead of span to avoid extra locals
            scoped ref T c1 = ref Component<T>.IsSparseComponent ?
                ref Unsafe.NullRef<T>() :
                ref archetype.GetComponentDataReference<T>();

            ref EntityIDOnly entity = ref archetype.GetEntityDataReference();

            for (int i = 0; i < archetype.EntityCount; i++)
            {
                int id = entity.ID;
                ref Bitset sparseBits = ref (uint)i < (uint)bitset.Length
                    ? ref bitset[i]
                    : ref Bitset.Zero;

                if (!Bitset.Filter(ref sparseBits, includeBits.AsVector(), excludeBits.AsVector()))
                    goto NextEntity;

                if (Component<T>.IsSparseComponent)
                {
                    if (!((uint)id < (uint)sparseArgArray.Length)) goto NextEntity;
                    int index = sparseArgArray[id];
                    if (index < 0) goto NextEntity;
                    c1 = ref Unsafe.Add(ref sparseFirst, index);
                }

                action.Run(ref c1);

            NextEntity:
                entity = ref Unsafe.Add(ref entity, 1);
                if (!Component<T>.IsSparseComponent) c1 = ref Unsafe.Add(ref c1, 1);
            }
        }
    }
}
