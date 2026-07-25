using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Systems;

/// <summary>
/// Enumerates the component references of every <see cref="Entity"/> on the far side of a link.
/// </summary>
/// <remarks>Linked entities that do not have every enumerated component are skipped.</remarks>
/// <variadic />
[Variadic(AttributeHelpers.LinkEnumerator)]
public ref struct LinkEnumerator<T>
{
    private readonly World _world;
    private Span<Archetype> _archetypes;
    private Span<int> _rows;
    private int _index;

    private int _currentRow;
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

    internal LinkEnumerator(Entity entity, LinkID linkID, int incoming)
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
    public RefTuple<T> Current => new()
    {
#if NETSTANDARD
        Item1 = Component<T>.IsSparseComponent ?
            MemoryHelpers.GetSparseSet<T>(ref MemoryMarshal.GetReference(_sparseSets)).GetUnsafe(_currentEntityID) :
            new Ref<T>(_c1Span, _currentRow),
#else
        Item1 = Component<T>.IsSparseComponent ?
            MemoryHelpers.GetSparseSet<T>(ref _sparseFirst).GetUnsafe(_currentEntityID) :
            new Ref<T>(ref Unsafe.Add(ref _base.Item1.RawRef, _currentRow)),
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

            _currentEntityID = currentArchetype.GetEntitySpan()[_currentRow].ID;

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
}

/// <summary>
/// A wrapper over one side of an <see cref="Entity"/>'s links for enumeration.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.LinkEnumerator)]
public readonly struct LinkEnumerable<T>
{
    private readonly Entity _entity;
    private readonly LinkID _linkID;
    private readonly int _incoming;

    internal LinkEnumerable(Entity entity, LinkID linkID, int incoming)
    {
        _entity = entity;
        _linkID = linkID;
        _incoming = incoming;
    }

    /// <summary>
    /// Gets the enumerator over the linked entities.
    /// </summary>
    public LinkEnumerator<T> GetEnumerator() => new(_entity, _linkID, _incoming);
}

internal static class LinkFilter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TComponent>(Archetype archetype, int row)
    {
        if (Component<TComponent>.IsSparseComponent)
            return archetype.GetBitsetNoLazy(row).IsSet(Component<TComponent>.SparseSetComponentIndex);
        return archetype.GetComponentIndex<TComponent>() != 0;
    }
}
