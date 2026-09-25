using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Frent.Systems;

internal sealed class QueryImpl
{
    internal Span<Archetype> AsSpan() => _archetypes.AsSpan();

    internal int ArchetypeCount => _archetypes.Count;

    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(2);
    private ImmutableArray<Rule> _archetypicalRules;
    private ImmutableArray<Rule> _sparseRules;

    private readonly Bitset _hasSparseComponents;
    private readonly Bitset _excludeSparseComponents;
    internal readonly bool HasSparseExclusions = false;
    internal readonly bool HasSparseRules = false;

    internal Bitset ExcludeMask => _excludeSparseComponents;
    internal Bitset IncludeMask => _hasSparseComponents;

    internal World World { get; init; }
    internal bool IncludeDisabled { get; init; }

    internal QueryImpl(World world, ImmutableArray<Rule> rules)
    {
        World = world;
        var builderSparse = ImmutableArray.CreateBuilder<Rule>();
        var builderArch = ImmutableArray.CreateBuilder<Rule>();
        foreach (var rule in rules)
        {
            if (rule.IsSparseRule)
            {
                HasSparseRules = true;

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

#if !NETSTANDARD
    internal ref Archetype GetArchetypeDataReference() => ref _archetypes.GetDataReference();
#endif

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
        FrentExceptions.Throw_NullReferenceException();
    }
}
