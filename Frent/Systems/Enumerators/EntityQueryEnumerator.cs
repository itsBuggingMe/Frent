using Frent.Core;
using Frent.Variadic.Generator;
using Frent.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace Frent.Systems;

/// <summary>
/// Enumerates all component references of the specified types and the <see cref="Entity"/> instance for each <see cref="Entity"/> in a query.
/// </summary>
/// <variadic />
[Variadic("    private Span<T> _currentSpan1;", "|    private Span<T$> _currentSpan$;\n|")]
[Variadic("        Item1 = new Ref<T>(_currentSpan1, _componentIndex),",
    "|        Item$ = new Ref<T$>(_currentSpan$, _componentIndex),\n|")]
[Variadic("                _currentSpan1 = cur.GetComponentSpan<T>();",
    "|                _currentSpan$ = cur.GetComponentSpan<T$>();\n|")]
[Variadic("<T>", "<|T$, |>")]
public ref struct EntityQueryEnumerator<T>
{
    private int _archetypeIndex;
    private int _componentIndex;
    private World _world;
    private Span<Archetype> _archetypes;
    private Span<EntityIDOnly> _entityIds;
    private Span<T> _currentSpan1;
    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        _archetypeIndex = -1;
    }

    /// <summary>
    /// The current tuple of component references and the <see cref="Entity"/> instance.
    /// </summary>
    public EntityRefTuple<T> Current => new()
    {
        Entity = _entityIds[_componentIndex].ToEntity(_world),
        Item1 = new Ref<T>(_currentSpan1, _componentIndex),
    };

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
        if (++_componentIndex < _currentSpan1.Length)
        {
            return true;
        }

        do
        {
            _componentIndex = 0;
            _archetypeIndex++;

            if ((uint)_archetypeIndex < (uint)_archetypes.Length)
            {
                var cur = _archetypes[_archetypeIndex];
                _entityIds = cur.GetEntitySpan();
                _currentSpan1 = cur.GetComponentSpan<T>();
            }
            else
            {
                return false;
            }
        } while (!(_componentIndex < _currentSpan1.Length));

        return true;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public EntityQueryEnumerator<T> GetEnumerator() => this;
}

#if NETSTANDARD
/// <summary>
/// An enumerator that can be used to enumerate all <see cref="Entity"/> instances in a <see cref="Query"/>.
/// </summary>
public ref struct EntityQueryEnumerator
{
    private readonly World _world;
    private readonly Span<Archetype> _archetypes;
    private Archetype? _current;
    private EntityIDOnly[] _currentEntity;

    private int _currentArchetypeIndex;
    private int _currentEntityIndex;

    private readonly Bitset _include;
    private readonly Bitset _exclude;
    private readonly Bitset[]? _entityBitsets;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        
        _currentArchetypeIndex = -1;
        _currentEntityIndex = int.MaxValue - 1;

        _include = query.IncludeMask;
        _exclude = query.ExcludeMask;

        _entityBitsets = query.HasSparseRules ?
            _world.SparseComponentTable
            : null;

        _currentEntity = [];
    }
    
    /// <summary>
    /// The current <see cref="Entity"/> instance.
    /// </summary>
    public Entity Current => _currentEntity[_currentEntityIndex].ToEntity(_world);

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
            if (_entityBitsets is not null)
            {
                ref Bitset set = ref _entityBitsets[_currentEntity[_currentEntityIndex].ID];
                if (!Bitset.Filter(ref set, _include, _exclude))
                    continue;
            }

            return true;
        }

        if (++_currentArchetypeIndex < _archetypes.Length)
        {
            _current = _archetypes[_currentArchetypeIndex];
            _currentEntity = _current.EntityIDArray;
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
#else
/// <summary>
/// An enumerator that can be used to enumerate all <see cref="Entity"/> instances in a <see cref="Query"/>.
/// </summary>
public ref struct EntityQueryEnumerator
{
    private World _world;
    private ref Archetype _current;
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

        _current = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
        _archetypesLeft = query.ArchetypeCount;
        _entityBitsetsFirst = ref query.HasSparseRules ?
            ref MemoryMarshal.GetArrayDataReference(_world.SparseComponentTable)
            : ref Unsafe.NullRef<Bitset>();
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
            _current = ref Unsafe.Add(ref _current, 1);
            _entitiesLeft = _current.EntityCount;
            // point to index -1
            _currentEntity = ref Unsafe.Subtract(ref _current.GetEntityDataReference(), 1);

            goto BeginConsumeEntities;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public EntityQueryEnumerator GetEnumerator() => this;
}
#endif