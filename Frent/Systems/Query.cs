using Frent.Collections;
using Frent.Core;
using System.Collections;
using System.Collections.Immutable;

namespace Frent.Systems;

public class Query(Rule[] rules) : IEnumerable<Archetype>
{
    private FastStack<Archetype> _archetypes = FastStack<Archetype>.Create(1);
    private Rule[] _rules = rules;

    public FastStack<Archetype>.FastStackEnumerator GetEnumerator() => _archetypes.GetEnumerator();
    IEnumerator<Archetype> IEnumerable<Archetype>.GetEnumerator() => _archetypes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _archetypes.GetEnumerator();

    public void TryAttachArchetype(Archetype archetype)
    {
        if (ArchetypeSatisfiesQuery(archetype))
            _archetypes.Push(archetype);
    }

    private bool ArchetypeSatisfiesQuery(Archetype archetype)
    {
        foreach (var rule in _rules)
        {
            if (!RuleApplies(rule, archetype.ArchetypeTypeArray))
            {
                return false;
            }
        }
        return true;
    }


    private bool RuleApplies(Rule rule, ImmutableArray<Type> types)
    {
        if (rule.CustomOperator is not null)
        {
            return rule.CustomOperator(types.AsSpan());
        }

        return rule.RuleTypes switch
        {
            RuleTypes.Have => types.IndexOf(rule.Type) > -1,
            RuleTypes.DoesNotHave => types.IndexOf(rule.Type) == -1,
            _ => throw new Exception($"Invalid enum option {rule.RuleTypes}"),
        };
    }
}

public delegate bool CustomQueryDelegate(ReadOnlySpan<Type> archetypeTypes);