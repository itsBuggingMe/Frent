using Frent.Systems;
using System.Runtime.InteropServices;

namespace Frent;

partial class World
{
    /// <summary>
    /// Creates a custom query from the given set of rules. For an entity to be queried, all rules must apply
    /// </summary>
    /// <param name="rules">The rules governing which entities are queried</param>
    /// <returns>A query object representing all the entities that satisfy all the rules</returns>
    public Query CustomQuery(params Rule[] rules)
    {
        QueryHash queryHash = QueryHash.New();
        foreach (Rule rule in rules)
            queryHash.AddRule(rule);

        return CollectionsMarshal.GetValueRefOrAddDefault(QueryCache, queryHash.ToHashCode(), out _) ??= CreateQuery([.. rules]);
    }
}