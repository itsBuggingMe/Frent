using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Enumerates all component references of the specified types for each <see cref="Entity"/> in a query in chunks.
/// </summary>
[Variadic("                Span = cur.GetComponentSpan<T>(),",
    "|                Span$ = cur.GetComponentSpan<T$>(),\n|")]
[Variadic("<T>", "<|T$, |>")]
public ref struct ChunkQueryEnumerator<T>
{
    private World _world;
    private Span<Archetype> _archetypes;
    private int _archetypeIndex;
    private ChunkQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        _archetypeIndex = -1;
    }

    /// <summary>
    /// The current tuple of component chunks.
    /// </summary>
    public ChunkTuple<T> Current
    {
        get
        {
            Archetype cur = _archetypes[_archetypeIndex];
            return new()
            {
                Span = cur.GetComponentSpan<T>(),
            };
        }
    }

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next chunk of components in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext() => ++_archetypeIndex < _archetypes.Length;

    /// <summary>
    /// Proxy type for foreach syntax
    /// </summary>
    /// <param name="query">The query to wrap.</param>
    public struct QueryEnumerable(Query query)
    {
        /// <summary>
        /// Gets the enumerator over a query.
        /// </summary>
        public ChunkQueryEnumerator<T> GetEnumerator() => new(query);
    }
}