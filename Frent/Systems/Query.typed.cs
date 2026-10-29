using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// A query over entities with the specified component types, which can be enumerated directly in foreach loops.
/// </summary>
/// <variadic />
[Variadic("<T>", "<|T$, |>", 8)]
public partial class Query<T> : Query
{
    internal Query(QueryImpl impl) : base(impl)
    {

    }

    /// <summary>
    /// Gets an enumerator over the component references of all entities in this query. Allows this query to be used directly in foreach loops.
    /// </summary>
    public QueryEnumerator<T> GetEnumerator() => new(Impl);
    /// <summary>
    /// Enumerates component references and <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    public EntityQueryEnumerator<T>.Enumerable WithEntities() => EnumerateWithEntities<T>();
}
