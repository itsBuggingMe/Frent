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
    private Archetype? _currentArchetype;
    private EntityIDOnly[] _currentEntity;

    private int _currentArchetypeIndex;
    private int _currentEntityIndex;

    private readonly Bitset _include;
    private readonly Bitset _exclude;

    private Entity _current;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        
        _currentArchetypeIndex = -1;
        _currentEntityIndex = int.MaxValue - 1;

        _include = query.IncludeMask;
        _exclude = query.ExcludeMask;

        _currentEntity = [];
        _current = _world.DefaultWorldEntity;
    }
    
    /// <summary>
    /// The current <see cref="Entity"/> instance.
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

        while (++_currentEntityIndex < _currentEntity.Length)
        {// a okay
            _currentEntity[_currentEntityIndex].SetEntity(ref _current);



            //if (_entityBitsets is not null)
            //{
            //    ref Bitset set = ref _entityBitsets[_current.EntityID];
            //    if (!Bitset.Filter(ref set, _include, _exclude))
            //        continue;
            //}

            return true;
        }

        if (++_currentArchetypeIndex < _archetypes.Length)
        {
            _currentArchetype = _archetypes[_currentArchetypeIndex];
            _currentEntity = _currentArchetype.EntityIDArray;
            _currentEntityIndex = -1;

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
public ref struct EntityQueryEnumerator<T>
{
    private readonly World _world;
    private readonly Span<Archetype> _archetypes;
    private Archetype? _currentArchetype;
    private EntityIDOnly[] _currentEntity;

    private int _currentArchetypeIndex;
    private int _currentEntityIndex;

    private readonly Bitset _include;
    private readonly Bitset _exclude;

    private EntityRefTuple<T> _current;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();

        _currentArchetypeIndex = -1;
        _currentEntityIndex = int.MaxValue - 1;

        _include = query.IncludeMask;
        _exclude = query.ExcludeMask;

        _currentEntity = [];
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

        while (++_currentEntityIndex < _currentEntity.Length)
        {// a okay
            //if (_entityBitsets is not null)
            //{
            //    ref Bitset set = ref _entityBitsets[_currentEntity[_currentEntityIndex].ID];
            //    if (!Bitset.Filter(ref set, _include, _exclude))
            //        continue;
            //}

            return true;
        }

        if (++_currentArchetypeIndex < _archetypes.Length)
        {
            _currentArchetype = _archetypes[_currentArchetypeIndex];
            _currentEntity = _currentArchetype.EntityIDArray;
            _currentEntityIndex = -1;

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

    private int _archetypesLeft;
    private int _entitiesLeft;

    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;
    private readonly ref Bitset _entityBitsetsFirst;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _include = query.IncludeMask.AsVector();
        _exclude = query.ExcludeMask.AsVector();

        _currentArchetype = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
        _archetypesLeft = query.ArchetypeCount;
    }
    
    /// <summary>
    /// The current <see cref="Entity"/> instance.
    /// </summary>
    public Entity Current => _currentEntity.ToEntity(_world);

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
            _currentEntity = ref Unsafe.Add(ref _currentEntity, 1);
            if (!Unsafe.IsNullRef(ref _entityBitsetsFirst))
            {
                ref Bitset set = ref Unsafe.Add(ref _entityBitsetsFirst, _currentEntity.ID);
                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            return true;
        }

        if (--_archetypesLeft >= 0)
        {
            _currentArchetype = ref Unsafe.Add(ref _currentArchetype, 1);
            _entitiesLeft = _currentArchetype.EntityCount;
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
public ref struct EntityQueryEnumerator<T>
{
    private readonly World _world;
    private ref Archetype _currentArchetype;
    private ref EntityIDOnly _currentEntity;

    private int _archetypesLeft;
    private int _entitiesLeft;

    private EntityRefTuple<T> _current;

    private readonly System.Runtime.Intrinsics.Vector256<ulong> _include;
    private readonly System.Runtime.Intrinsics.Vector256<ulong> _exclude;
    private readonly ref Bitset _entityBitsetsFirst;
    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _include = query.IncludeMask.AsVector();
        _exclude = query.ExcludeMask.AsVector();

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
            _currentEntity = ref Unsafe.Add(ref _currentEntity, 1);

            _current.Entity = _currentEntity.ToEntity(_world);
            _current.Item1.RawRef = ref Unsafe.Add(ref _current.Item1.RawRef, 1);

            if (!Unsafe.IsNullRef(ref _entityBitsetsFirst))
            {
                ref Bitset set = ref Unsafe.Add(ref _entityBitsetsFirst, _currentEntity.ID);
                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            return true;
        }

        if (--_archetypesLeft >= 0)
        {
            _currentArchetype = ref Unsafe.Add(ref _currentArchetype, 1);
            _entitiesLeft = _currentArchetype.EntityCount;
            // point to index -1
            _currentEntity = ref Unsafe.Subtract(ref _currentArchetype.GetEntityDataReference(), 1);

            _current.Item1.RawRef = ref Unsafe.Subtract(ref _currentArchetype.GetComponentDataReference<T>(), 1);

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