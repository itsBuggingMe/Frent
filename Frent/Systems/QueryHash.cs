namespace Frent.Systems;
internal struct QueryHash
{
    public QueryHash() { }
    private HashCode _state = new HashCode();

    public static QueryHash New() => new QueryHash();

    public QueryHash AddRule(Rule rule)
    {
        _state.Add(rule);
        return this;
    }

    public int ToHashCode() => _state.ToHashCode();
}
