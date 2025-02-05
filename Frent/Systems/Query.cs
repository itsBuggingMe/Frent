using Frent.Collections;
using Frent.Core;
using System.Collections.Immutable;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Represents a set of entities from a world which can have systems applied to
/// </summary>
public partial class Query
{
    internal Span<Archetype> AsSpan() => _archetypes.AsSpan();

    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(2);
    private ImmutableArray<Rule> _rules;
    internal World World { get; private set; }

    internal Query(World world, ImmutableArray<Rule> rules)
    {
        World = world;
        _rules = rules;
    }

    internal void TryAttachArchetype(Archetype archetype)
    {
        if (ArchetypeSatisfiesQuery(archetype.ID))
            _archetypes.Push(archetype);
    }

    private bool ArchetypeSatisfiesQuery(ArchetypeID id)
    {
        foreach (var rule in _rules)
        {
            if (!rule.RuleApplies(id))
            {
                return false;
            }
        }
        return true;
    }
}

[Variadic("<T>","<|T$, |>")]
partial class Query
{
    public QueryEnumerator<T>.QueryEnumerable Enumerate<T>() => new(this);
    public EntityQueryEnumerator<T>.QueryEnumerable EnumerateWithEntities<T>() => new(this);
}