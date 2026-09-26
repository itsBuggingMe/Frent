using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Variadic.Generator;

#if !NETSTANDARD
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace Frent.Systems;

/// <summary>
/// A query over entities with the specified component types, which can be enumerated directly in foreach loops.
/// </summary>
/// <variadic />
[Variadic("SingleQueryEnumerator", "QueryEnumerator<|T$, |>")]
[Variadic("<T>", "<|T$, |>")]
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

#endif

#if NETSTANDARD
        private Span<ComponentSparseSetBase> _sparseSets;
#else
        private ref T _base;

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

#pragma warning disable CS8618
        internal SingleQueryEnumerator(QueryImpl query)
#pragma warning restore CS8618
        {
            query.AssertHasSparseComponent<T>();

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
        public readonly ref T Current =>
#if NETSTANDARD
            ref Component<T>.IsSparseComponent ?
                ref MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).GetUnsafe(_currentEntityID).Value :
                ref _c1Span.UnsafeSpanIndex(_entityIndex)
#else
            ref Component<T>.IsSparseComponent ?
                ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetUnsafe(_currentEntityID).RawRef :
                ref Unsafe.Add(ref _base, _entityIndex)
#endif
            ;

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
                _base = ref Component<T>.IsSparseComponent ?
                    ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetComponentDataReference() :
                    ref currentArchetype.GetComponentDataReference<T>();
#endif

                goto BeginConsumeEntities;
            }

            return false;
        }
    }
}