using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// A query over entities with the specified component types, which can be enumerated directly in foreach loops.
/// </summary>
/// <variadic />
[Variadic("SingleQueryEnumerator", "QueryEnumerator<|T$, |>", 8)]
[Variadic("<T>", "<|T$, |>", 8)]
public partial class Query<T> : Query
{
    internal Query(QueryImpl impl) : base(impl)
    {

    }

    /// <summary>
    /// Gets an enumerator over the component references of all entities in this query. Allows this query to be used directly in foreach loops.
    /// </summary>
    public SingleQueryEnumerator GetEnumerator() => new(Impl);
    /// <summary>
    /// Enumerates component references and <see cref="Entity"/> instances for all entities in this query. Intended for use in foreach loops.
    /// </summary>
    public EntityQueryEnumerator<T>.Enumerable WithEntities() => EnumerateWithEntities<T>();
}

partial class Query<T>
{

    /// <summary>
    /// Enumerates the <typeparamref name="T"/> component reference for each <see cref="Entity"/> in a query.
    /// </summary>
    public ref struct SingleQueryEnumerator
    {
        private QueryEnumerator<T> _inner;

        internal SingleQueryEnumerator(QueryImpl query)
        {
            _inner = new(query);
        }

        /// <summary>
        /// A reference to the current component.
        /// </summary>
        public ref T Current => ref _inner.Current.Item1.Value;

        /// <summary>
        /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
        /// </summary>
        public void Dispose() => _inner.Dispose();

        /// <summary>
        /// Moves to the next component in this enumeration.
        /// </summary>
        /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
        public bool MoveNext() => _inner.MoveNext();
    }
}