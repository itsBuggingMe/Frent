using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Systems;

/// <summary>
/// Enumerates the component references and the <see cref="Entity"/> instance of every <see cref="Entity"/> on the far side of a link.
/// </summary>
/// <remarks>Linked entities that do not have every enumerated component are skipped.</remarks>
/// <variadic />
[Variadic(AttributeHelpers.LinkEnumerator)]
public ref struct EntityLinkEnumerator<T>
{
    private readonly World _world;
    private Span<Archetype> _archetypes;
    private Span<int> _rows;
    private int _index;

    private int _currentRow;
    private Entity _current;

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

    internal EntityLinkEnumerator(Entity entity, LinkID linkID, bool incoming)
    {
        ref EntityLocation location = ref entity.AssertIsAlive(out World world);

        _world = world;

#if NETSTANDARD
        _sparseSets = world.WorldSparseSetTable;
#else
        _sparseFirst = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);
#endif
        _world.EnterDisallowState();

        LinkTable.GetLinkedSlots(world, linkID, ref location, incoming, out _archetypes, out _rows);
        _index = -1;
    }

    /// <summary>
    /// The current tuple of component references.
    /// </summary>
    public EntityRefTuple<T> Current => new()
    {
        Entity = _current,
#if NETSTANDARD
        Item1 = Component<T>.IsSparseComponent ?
            MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).GetUnsafe(_current.EntityID) :
            new Ref<T>(_c1Span, _currentRow),
#else
        Item1 = new Ref<T>(ref Component<T>.IsSparseComponent ?
            ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetUnsafe(_current.EntityID).RawRef :
            ref Unsafe.Add(ref _base.Item1.RawRef, _currentRow)),
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
    /// Moves to the next linked <see cref="Entity"/> in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        while ((uint)++_index < (uint)_archetypes.Length)
        {
            Archetype currentArchetype = _archetypes.UnsafeSpanIndex(_index);
            _currentRow = _rows.UnsafeSpanIndex(_index);

            if (!LinkFilter.Has<T>(currentArchetype, _currentRow))
                continue;

            _current = currentArchetype.GetEntitySpan().UnsafeSpanIndex(_currentRow).ToEntity(_world);

#if NETSTANDARD
            _c1Span = Component<T>.IsSparseComponent ?
                MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).Dense :
                currentArchetype.GetComponentSpan<T>();
#else
            _base.Item1.RawRef = ref Component<T>.IsSparseComponent ?
                ref MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetComponentDataReference() :
                ref currentArchetype.GetComponentDataReference<T>();
#endif

            return true;
        }

        return false;
    }

    /// <summary>
    /// A wrapper over one side of an <see cref="Entity"/>'s links for enumeration with entities.
    /// </summary>
    public readonly struct Enumerable
    {
        private readonly Entity _entity;
        private readonly LinkID _linkID;
        private readonly bool _incoming;

        internal Enumerable(Entity entity, LinkID linkID, bool incoming)
        {
            _entity = entity;
            _linkID = linkID;
            _incoming = incoming;
        }

        /// <summary>
        /// Gets the enumerator over the linked entities.
        /// </summary>
        public EntityLinkEnumerator<T> GetEnumerator() => new(_entity, _linkID, _incoming);
    }
}

/// <summary>
/// Enumerates every <see cref="Entity"/> on the far side of a link.
/// </summary>
/// <remarks>Unlike the generic overloads, no linked entity is skipped since there are no components to filter on.</remarks>
public ref struct EntityLinkEnumerator
{
    private readonly World _world;
    private Span<Archetype> _archetypes;
    private Span<int> _rows;
    private int _index;

    private Entity _current;

    internal EntityLinkEnumerator(Entity entity, LinkID linkID, bool incoming)
    {
        ref EntityLocation location = ref entity.AssertIsAlive(out World world);

        _world = world;
        _world.EnterDisallowState();

        LinkTable.GetLinkedSlots(world, linkID, ref location, incoming, out _archetypes, out _rows);
        _index = -1;
    }

    /// <summary>
    /// The current linked <see cref="Entity"/>.
    /// </summary>
    public Entity Current => _current;

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next linked <see cref="Entity"/> in this enumeration.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (!((uint)++_index < (uint)_archetypes.Length))
            return false;

        Archetype currentArchetype = _archetypes.UnsafeSpanIndex(_index);
        _current = currentArchetype.GetEntitySpan().UnsafeSpanIndex(_rows.UnsafeSpanIndex(_index)).ToEntity(_world);

        return true;
    }

    /// <summary>
    /// A wrapper over one side of an <see cref="Entity"/>'s links for enumeration of entities alone.
    /// </summary>
    public readonly struct Enumerable
    {
        private readonly Entity _entity;
        private readonly LinkID _linkID;
        private readonly bool _incoming;

        internal Enumerable(Entity entity, LinkID linkID, bool incoming)
        {
            _entity = entity;
            _linkID = linkID;
            _incoming = incoming;
        }

        /// <summary>
        /// Gets the enumerator over the linked entities.
        /// </summary>
        public EntityLinkEnumerator GetEnumerator() => new(_entity, _linkID, _incoming);
    }
}
