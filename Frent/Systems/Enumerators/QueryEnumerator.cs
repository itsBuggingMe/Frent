using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent.Systems;

#if NETSTANDARD
/// <summary>
/// Enumerates all component references of the specified types and the <see cref="Entity"/> instance for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.QueryEnumerator)]
public ref struct QueryEnumerator<T>
{
    private readonly World _world;
    private readonly Span<Archetype> _archetypes;
    private Span<EntityIDOnly> _entities;
    private Span<T> _compSpan1;

    private readonly ref Archetype _currentArchetype => ref _archetypes[_archetypesIndex];
    private readonly ref EntityIDOnly _currentEntity => ref _entities[_entitiesIndex];

    private Span<Bitset> _archetypeBitsets;

    private int _archetypesIndex;
    private int _entitiesIndex;

    private RefTuple<T> _current;

    private readonly Bitset _include;
    private readonly Bitset _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal QueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : -1;
        if (query.HasSparseRules)
        {
            _include = query.IncludeMask;
            _exclude = query.ExcludeMask;
        }

        _archetypes = query.AsSpan();
        _archetypesIndex = -1;
    }

    /// <summary>
    /// The current tuple of component references and the <see cref="Entity"/> instance.
    /// </summary>
    public readonly RefTuple<T> Current => _current;

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public readonly void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next entity and its components in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
    BeginConsumeEntities:

        while (++_entitiesIndex < _archetypes.Length)
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

            int entityId = _currentEntity.ID;

            ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(_world.WorldSparseSetTable);

            _current.Item1 = Component<T>.IsSparseComponent
                ? MemoryHelpers.GetSparseSet<T>(ref first).GetUnsafe(entityId)
                : new Ref<T>(_compSpan1, _entitiesIndex);

            return true;
        }

        if ((uint)++_archetypesIndex < (uint)_archetypes.Length)
        {
            if (_sparseIndex >= 0)
            {
                _archetypeBitsets = _currentArchetype.SparseBitsetSpan();
            }

            _entities = _currentArchetype.GetEntitySpan();
            if(!Component<T>.IsSparseComponent) _compSpan1 = _currentArchetype.GetComponentSpan<T>();

            // point to index -1
            _entitiesIndex = -1;

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public QueryEnumerator<T> GetEnumerator() => this;
}
#else
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
            if (!Component<T>.IsSparseComponent) _current.Item1.RawRef = ref Unsafe.Subtract(ref _currentArchetype.GetComponentDataReference<T>(), 1);


            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public QueryEnumerator<T> GetEnumerator() => this;
}
#endif