using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Frent.Systems;
public static partial class WorldQueryExtensions
{
    /// <summary>
    /// Creates a custom query from the given set of rules. For an entity to be queried, all rules must apply
    /// </summary>
    /// <param name="rules">The rules governing which entities are queried</param>
    /// <param name="world">The world to query on</param>
    /// <returns>A query object representing all the entities that satisfy all the rules</returns>
    public static Query CustomQuery(this World world, params Rule[] rules)
    {
        ArgumentNullException.ThrowIfNull(world);

        QueryHash queryHash = QueryHash.New();
        foreach (Rule rule in rules)
            queryHash.AddRule(rule);

        return CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, queryHash.ToHashCodeIncludeDisable(), out _) ??= world.CreateQueryFromSpan([.. rules]);
    }

    //we could use static abstract methods IF NOT FOR DOTNET6
    public static Query Query<T>(this World world)
        where T : struct, IConstantQueryHashProvider
    {
        ref Query? cachedValue = ref CollectionsMarshal.GetValueRefOrAddDefault(world.QueryCache, default(T).GetHashCode(), out bool exists);
        if (!exists)
        {
            cachedValue = world.CreateQuery(default(T).Rules);
        }
        return cachedValue!;
    }
}