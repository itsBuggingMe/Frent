using System.Collections.Immutable;

namespace Frent.Systems;
internal struct QueryHash
{
    public QueryHash() { }
    private int _state = 12582917;

    public static QueryHash New() => new QueryHash();
    public static QueryHash New(ImmutableArray<Rule> rules)
    {
        var hash = new QueryHash();
        foreach (var rule in rules)
        {
            hash.AddRule(rule);
        }
        return hash;
    }

    public QueryHash AddRule(Rule rule)
    {
        _state *= rule.GetHashCode();
        return this;
    }

    public int ToHashCode() => _state;
}