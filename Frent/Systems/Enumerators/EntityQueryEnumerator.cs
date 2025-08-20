using Frent.Core;
using Frent.Variadic.Generator;
using Frent.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace Frent.Systems;

#if NETSTANDARD
/// <summary>
/// An enumerator that can be used to enumerate all <see cref="Entity"/> instances in a <see cref="Query"/>.
/// </summary>
public ref struct EntityQueryEnumerator
{
    private readonly World _world;
    private readonly Span<Archetype> _archetypes;
    private Span<EntityIDOnly> _entities;


    private readonly ref Archetype _currentArchetype => ref _archetypes[_archetypesIndex];
    private readonly ref EntityIDOnly _currentEntity => ref _entities[_entitiesIndex];

    private Span<Bitset> _archetypeBitsets;

    private int _archetypesIndex;
    private int _entitiesIndex;

    private Entity _current;

    private readonly Bitset _include;
    private readonly Bitset _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : int.MinValue;
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
    public readonly Entity Current => _current;

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public readonly void Dispose()
    {
        _world.ExitDisallowState(null);
    }

    /// <summary>
    /// Moves to the next entity.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
    BeginConsumeEntities:

        while (++_archetypesIndex < _archetypes.Length)
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

            _current = _currentEntity.ToEntity(_world);

            return true;
        }

        if ((uint)++_archetypesIndex < (uint)_archetypes.Length)
        {
            if (_sparseIndex >= 0)
            {
                _archetypeBitsets = _currentArchetype.SparseBitsetSpan();
                _sparseIndex = -1;
            }
            _entities = _currentArchetype.GetEntitySpan();

            // point to index -1
            _entitiesIndex = -1;

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public readonly EntityQueryEnumerator GetEnumerator() => this;
}

/// <summary>
/// Enumerates all component references of the specified types and the <see cref="Entity"/> instance for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.QueryEnumerator)]
public ref struct EntityQueryEnumerator<T>
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

    private EntityRefTuple<T> _current;

    private readonly Bitset _include;
    private readonly Bitset _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : int.MinValue;
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
    public readonly EntityRefTuple<T> Current => _current;

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

            _current.Entity = _currentEntity.ToEntity(_world);
            int entityId = _current.Entity.EntityID;

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
                _sparseIndex = -1;
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
    public EntityQueryEnumerator<T> GetEnumerator() => this;
}
#else
/// <summary>
/// An enumerator that can be used to enumerate all <see cref="Entity"/> instances in a <see cref="Query"/>.
/// </summary>
public ref struct EntityQueryEnumerator
{
    private readonly World _world;
    private ref Archetype _currentArchetype;
    private ref EntityIDOnly _currentEntity;
    private Span<Bitset> _archetypeBitsets;

    private int _archetypesLeft;
    private int _entitiesLeft;

    private Entity _current;

    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : int.MinValue;
        if (query.HasSparseRules)
        {
            _include = query.IncludeMask.AsVector();
            _exclude = query.ExcludeMask.AsVector();
        }

        _currentArchetype = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
        _archetypesLeft = query.ArchetypeCount;
    }

    /// <summary>
    /// The current tuple of component references and the <see cref="Entity"/> instance.
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
    /// Moves to the next entity.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext()
    {
    BeginConsumeEntities:

        while (--_entitiesLeft >= 0)
        {// a okay
            _sparseIndex++;

            _currentEntity = ref Unsafe.Add(ref _currentEntity, 1);

            if (_sparseIndex >= 0)
            {
                if (!((uint)_sparseIndex < (uint)_archetypeBitsets.Length))
                    continue;

                ref Bitset set = ref _archetypeBitsets[_sparseIndex];

                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            _current = _currentEntity.ToEntity(_world);

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
            _currentEntity = ref Unsafe.Subtract(ref _currentArchetype.GetEntityDataReference(), 1);

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public EntityQueryEnumerator GetEnumerator() => this;
}

/// <summary>
/// Enumerates all component references of the specified types and the <see cref="Entity"/> instance for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic(AttributeHelpers.QueryEnumerator)]
public ref struct EntityQueryEnumerator<T>
{
    private readonly World _world;
    private ref Archetype _currentArchetype;
    private ref EntityIDOnly _currentEntity;
    private Span<Bitset> _archetypeBitsets;

    private int _archetypesLeft;
    private int _entitiesLeft;

    private EntityRefTuple<T> _current;

    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;

    // inverse of _entitiesLeft
    private int _sparseIndex;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();

        _sparseIndex = query.HasSparseRules ? 0 : int.MinValue;
        if (query.HasSparseRules)
        {
            _include = query.IncludeMask.AsVector();
            _exclude = query.ExcludeMask.AsVector();
        }

        _currentArchetype = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
        _archetypesLeft = query.ArchetypeCount;
    }

    /// <summary>
    /// The current tuple of component references and the <see cref="Entity"/> instance.
    /// </summary>
    public EntityRefTuple<T> Current => _current;

    /// <summary>
    /// Indicates to the world that this enumeration is finished; the world might allow structual changes after this.
    /// </summary>
    public void Dispose()
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

        while (--_entitiesLeft >= 0)
        {// a okay
            _sparseIndex++;

            _currentEntity = ref Unsafe.Add(ref _currentEntity, 1);

            if (_sparseIndex >= 0)
            {
                if (!((uint)_sparseIndex < (uint)_archetypeBitsets.Length))
                    continue;

                ref Bitset set = ref _archetypeBitsets[_sparseIndex];

                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            _current.Entity = _currentEntity.ToEntity(_world);
            int entityId = _current.Entity.EntityID;
            // get ref to first sparse set
            // then use

            ref ComponentSparseSetBase first = ref (Component<T>.IsSparseComponent)
                ? ref MemoryMarshal.GetArrayDataReference(_world.WorldSparseSetTable)
                : ref Unsafe.NullRef<ComponentSparseSetBase>();

            _current.Item1.RawRef = ref !Component<T>.IsSparseComponent
                ? ref Unsafe.Add(ref _current.Item1.RawRef, 1)
                : ref MemoryHelpers.GetSparseSet<T>(ref first).GetUnsafe(entityId);

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
            _currentEntity = ref Unsafe.Subtract(ref _currentArchetype.GetEntityDataReference(), 1);

            if(!Component<T>.IsSparseComponent) _current.Item1.RawRef = ref Unsafe.Subtract(ref _currentArchetype.GetComponentDataReference<T>(), 1);

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public EntityQueryEnumerator<T> GetEnumerator() => this;
}
#endif