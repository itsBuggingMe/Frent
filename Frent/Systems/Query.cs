using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Frent.Systems;

/// <summary>
/// Represents a set of entities from a world which can have systems applied to
/// </summary>
public partial class Query
{
    internal Span<Archetype> AsSpan() => _archetypes.AsSpan();

    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(2);
    private ImmutableArray<Rule> _archetypicalRules;
    private ImmutableArray<Rule> _sparseRules;

    private Bitset _hasSparseComponents;
    private Bitset _excludeSparseComponents;
    internal readonly bool HasSparseExclusions = false;

    internal Bitset ExcludeMask => _excludeSparseComponents;

    internal World World { get; init; }
    internal bool IncludeDisabled { get; init; }

    internal Query(World world, ImmutableArray<Rule> rules)
    {
        World = world;
        var builderSparse = ImmutableArray.CreateBuilder<Rule>();
        var builderArch = ImmutableArray.CreateBuilder<Rule>();
        foreach (var rule in rules)
        {
            if(rule.IsSparseRule)
            {
                builderSparse.Add(rule);

                Debug.Assert(rule.SparseIndex != 0);
                Debug.Assert(rule.RuleStateValue == Rule.RuleState.HasComponent || 
                    rule.RuleStateValue == Rule.RuleState.NotComponent);
                
                ref Bitset toModify = ref rule.RuleStateValue == Rule.RuleState.HasComponent ?
                    ref _hasSparseComponents :
                    ref _excludeSparseComponents;
                
                toModify.Set(rule.SparseIndex);
            }
            else
                builderArch.Add(rule);
            IncludeDisabled |= rule == Rule.IncludeDisabledRule;
        }

        _sparseRules = builderSparse.ToImmutable();
        _archetypicalRules = builderArch.ToImmutable();

        HasSparseExclusions = !_excludeSparseComponents.IsDefault;
    }

    internal void TryAttachArchetype(Archetype archetype)
    {
        if (!IncludeDisabled && archetype.HasTag<Disable>())
            return;

        if (ArchetypeSatisfiesQuery(archetype.ID))
            _archetypes.Push(archetype);
    }

    private bool ArchetypeSatisfiesQuery(ArchetypeID id)
    {
        foreach (var rule in _archetypicalRules)
        {
            if (!rule.RuleApplies(id))
            {
                return false;
            }
        }
        return true;
    }

    internal void AssertHasSparseComponent<T>()
    {
        if (!Component<T>.IsSparseComponent)
            return;
        if (_hasSparseComponents.IsSet(Component<T>.SparseSetComponentIndex))
            return;

        // match behavior of when archetypical components are not includes
        Unsafe.NullRef<int>() = 0;
    }
}

/// <variadic />
[Variadic("<T>", "<|T$, |>")]
partial class Query
{
    /// <summary>
    /// Enumerates component references for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public QueryEnumerator<T> Enumerate<T>() => new(this);
    /// <summary>
    /// Enumerates component references and <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public EntityQueryEnumerator<T> EnumerateWithEntities<T>() => new(this);
    /// <summary>
    /// Enumerates component chunks for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    /// <variadic />
    public ChunkQueryEnumerator<T> EnumerateChunks<T>() => new(this);
}

partial class Query
{
    /// <summary>
    /// Enumerates <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    public EntityQueryEnumerator EnumerateWithEntities() => new(this);
}