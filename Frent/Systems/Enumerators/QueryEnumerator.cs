using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent.Systems;

/// <summary>
/// Enumerates all component references of the specified types for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.QueryEnumerator)]
public ref struct QueryEnumerator<T>
{
    private readonly World _world;
    private ref Archetype _currentArchetype;
    private Span<Bitset> _archetypeBitsets;

    private int _archetypesLeft;
    private int _entitiesLeft;

    private RefTuple<T> _current;

    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal QueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : -1;
        if (query.HasSparseRules)
        {
            _include = query.IncludeMask.AsVector();
            _exclude = query.ExcludeMask.AsVector();
        }

        _currentArchetype = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
        _archetypesLeft = query.ArchetypeCount;
    }

    /// <summary>
    /// The current tuple of component references.
    /// </summary>
    public RefTuple<T> Current => _current;

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the component tuple in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
    BeginConsumeEntities:

        while (--_entitiesLeft >= 0)
        {// a okay
            _sparseIndex++;

            if (_sparseIndex >= 0)
            {
                if (!((uint)_sparseIndex < (uint)_archetypeBitsets.Length))
                    continue;

                ref Bitset set = ref _archetypeBitsets[_sparseIndex];

                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            _current.Item1.RawRef = ref Unsafe.Add(ref _current.Item1.RawRef, 1);

            return true;
        }

        if (--_archetypesLeft >= 0)
        {
            _currentArchetype = ref Unsafe.Add(ref _currentArchetype, 1);
            _entitiesLeft = _currentArchetype.EntityCount;
            if (_sparseIndex >= 0)
            {
                _archetypeBitsets = _currentArchetype.SparseBitsetSpan();
                _sparseIndex = -1;
            }

            // point to index -1
            _current.Item1.RawRef = ref Unsafe.Subtract(ref _currentArchetype.GetComponentDataReference<T>(), 1);


            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public QueryEnumerator<T> GetEnumerator() => this;
}