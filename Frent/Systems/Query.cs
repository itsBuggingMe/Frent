using Frent.Collections;
using Frent.Core;
using System.Collections;
using System.Collections.Immutable;

namespace Frent.Systems;

/// <summary>
/// Represents a set of entities from a world which can have systems applied to
/// </summary>
public class Query
{
    internal Span<Archetype> AsSpan() => _archetypes.AsSpan();

    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(2);
    private Rule[] _rules;

    internal Query(Rule[] rules)
    {
        _rules = rules;
    }

    internal void TryAttachArchetype(Archetype archetype)
    {
        if (ArchetypeSatisfiesQuery(archetype))
            _archetypes.Push(archetype);
    }

    private bool ArchetypeSatisfiesQuery(Archetype archetype)
    {
        foreach (var rule in _rules)
        {
            if(!rule.RuleApplies(archetype.ID))
            {
                return false;
            }
        }
        return true;
    }
}