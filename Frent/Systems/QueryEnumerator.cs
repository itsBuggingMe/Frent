using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;

/// <summary>
/// Enumerates all component references of the specified types for each <see cref="Entity"/> in a query.
/// </summary>
[Variadic("    private Span<T> _currentSpan1;", "|    private Span<T$> _currentSpan$;\n|")]
[Variadic("        Item1 = new Ref<T>(_currentSpan1, _componentIndex),",
    "|        Item$ = new Ref<T$>(_currentSpan$,  _componentIndex),\n|")]
[Variadic("            _currentSpan1 = cur.GetComponentSpan<T>();",
    "|            _currentSpan$ = cur.GetComponentSpan<T$>();\n|")]
[Variadic("<T>", "<|T$, |>")]
public ref struct QueryEnumerator<T>
{
    private int _archetypeIndex;
    private int _componentIndex;
    private World _world;
    private Span<Archetype> _archetypes;
    private Span<T> _currentSpan1;
    private QueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        _archetypeIndex = -1;
    }

    /// <summary>
    /// The current tuple of component references.
    /// </summary>
    public RefTuple<T> Current => new()
    {
        Item1 = new Ref<T>(_currentSpan1, _componentIndex),
    };

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    /// <remarks>This MUST be called when finished with the enumerator!</remarks>
    public void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next entity set of entity references.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (++_componentIndex < _currentSpan1.Length)
        {
            return true;
        }

        _componentIndex = 0;
        _archetypeIndex++;

        while ((uint)_archetypeIndex < (uint)_archetypes.Length)
        {

            var cur = _archetypes[_archetypeIndex];
            _currentSpan1 = cur.GetComponentSpan<T>();

            if (!_currentSpan1.IsEmpty)
                return true;

            _archetypeIndex++;
        }

        return false;
    }

    /// <summary>
    /// Proxy type for foreach syntax
    /// </summary>
    /// <param name="query"></param>
    public struct QueryEnumerable(Query query)
    {
        /// <summary>
        /// Gets the enumerator over a query.
        /// </summary>
        public QueryEnumerator<T> GetEnumerator() => new QueryEnumerator<T>(query);
    }
}