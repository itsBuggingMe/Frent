using Frent.Collections;
using Frent.Core;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Systems;

/// <summary>
/// Enumerates all component references of the specified types for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.QueryEnumerator)]
public ref struct QueryEnumerator<T>
{
    private readonly World _world;
    private Span<Bitset> _archetypeBitsets;
    private Span<Archetype> _archetypes;
    private Span<EntityIDOnly> _entities;
    private int _archetypeIndex;
    private int _entityIndex;

    private int _currentEntityID;

#if NETSTANDARD
    private Span<T> _c1Span;
#else
    private RefTuple<T> _base;
#endif

#if NETSTANDARD
    private Span<ComponentSparseSetBase> _sparseSets;
#else
    private ref ComponentSparseSetBase _sparseFirst;
#endif


#if NETSTANDARD
    private readonly Bitset _include;
    private readonly Bitset _exclude;
#else
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;
#endif

    private bool _hasSparseRules;

    internal QueryEnumerator(Query query)
    {
        _world = query.World;

#if NETSTANDARD
        _sparseSets = query.World.WorldSparseSetTable;
#else
        _sparseFirst = ref MemoryMarshal.GetArrayDataReference(query.World.WorldSparseSetTable);
#endif
        _world.EnterDisallowState();

        if (query.HasSparseRules)
        {
            _hasSparseRules = true;

#if NETSTANDARD
            _include = query.IncludeMask;
            _exclude = query.ExcludeMask;
#else
            _include = query.IncludeMask.AsVector();
            _exclude = query.ExcludeMask.AsVector();
#endif
        }

        _archetypes = query.AsSpan();
        _entityIndex = int.MaxValue - 1;
        _archetypeIndex = -1;
    }

    /// <summary>
    /// The current tuple of component references.
    /// </summary>
    public RefTuple<T> Current => new()
    {
#if NETSTANDARD
        Item1 = Component<T>.IsSparseComponent ?
            MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).GetUnsafe(_currentEntityID) :
            new Ref<T>(_c1Span, _entityIndex),
#else
        Item1 = new Ref<T>(ref Component<T>.IsSparseComponent ?
            ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetUnsafe(_currentEntityID) :
            ref Unsafe.Add(ref _base.Item1.RawRef, _entityIndex)),
#endif
    };

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next component tuple in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
    BeginConsumeEntities:

        while ((uint)++_entityIndex < (uint)_entities.Length)
        {// a okay

            if (_hasSparseRules)
            {
                ref Bitset set = ref (uint)_entityIndex < (uint)_archetypeBitsets.Length
                    ? ref _archetypeBitsets[_entityIndex]
                    : ref Bitset.Zero;

                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            _currentEntityID = _entities[_entityIndex].ID;

            return true;
        }

        if ((uint)++_archetypeIndex < (uint)_archetypes.Length)
        {
            var currentArchetype = _archetypes[_archetypeIndex];
            _entities = currentArchetype.GetEntitySpan();
            _entityIndex = -1;

            if (_hasSparseRules)
            {
                _archetypeBitsets = currentArchetype.SparseBitsetSpan();
            }

#if NETSTANDARD
            _c1Span = Component<T>.IsSparseComponent ?
                MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).Dense :
                currentArchetype.GetComponentSpan<T>();
#else
            _base.Item1.RawRef = ref Component<T>.IsSparseComponent ?
                ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetComponentDataReference() :
                ref currentArchetype.GetComponentDataReference<T>();
#endif

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public QueryEnumerator<T> GetEnumerator() => this;
}