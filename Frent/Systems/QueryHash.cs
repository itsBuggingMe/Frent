namespace Frent.Systems;
internal struct QueryHash
{
    public QueryHash() { }
    private HashCode _state = new HashCode();
    public static QueryHash New() => new QueryHash();

    public QueryHash With<T>()
    {
        _state.Add(RuleTypes.Have);
        _state.Add(typeof(T));
        return this;
    }
    public QueryHash Not<T>()
    {
        _state.Add(RuleTypes.DoesNotHave);
        _state.Add(typeof(T));
        return this;
    }

    public void AddRule(Rule rule)
    {
        if (rule.CustomOperator is not null)
        {
            _state.Add(rule.CustomOperator);
            return;
        }

        _state.Add(rule.RuleTypes);
        _state.Add(rule.Type);
    }

    public int ToHashCode() => _state.ToHashCode();
}
