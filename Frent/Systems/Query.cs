using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Collections.Immutable;

namespace Frent.Systems;

/// <summary>
/// Represents a set of entities from a world which can have systems applied to
/// </summary>
public partial class Query
{
    internal Span<Archetype> AsSpan() => _archetypes.AsSpan();

    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(2);
    private ImmutableArray<Rule> _rules;
    internal World World { get; init; }
    internal bool IncludeDisabled { get; init; }

    internal Query(World world, ImmutableArray<Rule> rules)
    {
        World = world;
        _rules = rules;
        foreach (var rule in rules)
            if (rule == Rule.IncludeDisabledRule)
            {
                IncludeDisabled = true;
                break;
            }
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

[Variadic("<T>", "<|T$, |>")]
partial class Query
{
    public QueryEnumerator<T>.QueryEnumerable Enumerate<T>() => new(this);
    public EntityQueryEnumerator<T>.QueryEnumerable EnumerateWithEntities<T>() => new(this);
    public ChunkQueryEnumerator<T>.QueryEnumerable EnumerateChunks<T>() => new(this);
}

partial class Query
{
    public EntityQueryEnumerator.QueryEnumerable EnumerateWithEntities() => new(this);
}