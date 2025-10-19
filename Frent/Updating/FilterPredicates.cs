using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Core.Structures.GlobalWorldTables;
using static Frent.Entity;

namespace Frent.Updating;

/// <inheritdoc cref="GenerationServices"/>
public interface IFilterPredicate
{
    internal bool SkipEntity(ref byte entityLookup, ref readonly Bitset sparseBits);
}

internal record struct JoinPredicate<T1, T2, T3, T4> : IFilterPredicate
    where T1 : struct, IFilterPredicate
    where T2 : struct, IFilterPredicate
    where T3 : struct, IFilterPredicate
    where T4 : struct, IFilterPredicate
{
    public bool SkipEntity(ref byte entityLookup, ref readonly Bitset sparseBits) =>
           default(T1).SkipEntity(ref entityLookup, in sparseBits)
        || default(T2).SkipEntity(ref entityLookup, in sparseBits)
        || default(T3).SkipEntity(ref entityLookup, in sparseBits)
        || default(T4).SkipEntity(ref entityLookup, in sparseBits);
}

internal readonly struct NonePredicate : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits) => false;
}

[Variadic("!HasTag<T1>(ref e)", "|!HasTag<T$>(ref e) || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct IncludeTagsPredicate<T1> : IFilterPredicate
{
    // we only need to care about the sparse case here
    // bad archetypes are filtered out non-generically
    // since sparse tags dont exist
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        return !HasTag<T1>(ref e);
    }
}

[Variadic("!HasTag<T1>(ref e)", "|!HasTag<T$>(ref e) || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct ExcludeTagsPredicate<T1> : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        return HasTag<T1>(ref e);
    }
}

// for when TComponent is ISpareSet (TComponent is not T1), but the component the update method is on
[Variadic("Component<T1>.IsSparseComponent", "|Component<T$>.IsSparseComponent || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("HasComponent<T1>(ref e)", "|HasComponent<T$>(ref e) || |false", 8)]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct SparseIncludeComponentFilterPredicate<T1> : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        // precondition of being in an IRunner's sparse update method

        if((Component<T1>.IsSparseComponent)
            && !Bitset.FilterInclude(in sparseBits, BitsetHelper<T1>.BitsetOf.AsVector()))
        {
            return true;
        }

        // archetypical ones
        return HasComponent<T1>(ref e);
    }
}

[Variadic("Component<T1>.IsSparseComponent", "|Component<T$>.IsSparseComponent || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("HasComponent<T1>(ref e)", "|HasComponent<T$>(ref e) || |false")]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct ArchetypicalIncludeComponentFilterPredicate<T1> : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        if ((Component<T1>.IsSparseComponent)
            && !Bitset.FilterInclude(in sparseBits, BitsetHelper<T1>.BitsetOf.AsVector()))
        {
            return true;
        }
        return HasComponent<T1>(ref e);
    }
}

[Variadic("Component<T1>.IsSparseComponent", "|Component<T$>.IsSparseComponent || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("!HasComponent<T1>(ref e)", "|!HasComponent<T$>(ref e) || |false")]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct SparseExcludeComponentFilterPredicate<T1> : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        if ((Component<T1>.IsSparseComponent)
            && !Bitset.FilterExclude(in sparseBits, BitsetHelper<T1>.BitsetOf.AsVector()))
        {
            return true;
        }

        return !HasComponent<T1>(ref e);
    }
}

[Variadic("Component<T1>.IsSparseComponent", "|Component<T$>.IsSparseComponent || |false", 8)]
[Variadic("BitsetHelper<T1>", "BitsetHelper<|T$, |>")]
[Variadic("!HasComponent<T1>(ref e)", "|!HasComponent<T$>(e) || |false")]
[Variadic("Predicate<T1>", "Predicate<|T$, |>")]
internal readonly struct ArchetypicalExcludeComponentFilterPredicate<T1> : IFilterPredicate
{
    public readonly bool SkipEntity(ref byte e, ref readonly Bitset sparseBits)
    {
        if ((Component<T1>.IsSparseComponent)
            && !Bitset.FilterExclude(in sparseBits, BitsetHelper<T1>.BitsetOf.AsVector()))
        {
            return true;
        }
        return HasComponent<T1>(ref e);
    }
}