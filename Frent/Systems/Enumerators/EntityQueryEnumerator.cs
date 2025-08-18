using Frent.Core;
using Frent.Variadic.Generator;
using Frent.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Runtime.Intrinsics;

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

    private readonly Vector256<ulong> _include;
    private readonly Vector256<ulong> _exclude;
    private readonly bool _checkSparse;

    internal EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _current = ref Unsafe.Subtract(ref query.GetArchetypeDataReference(), 1);
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
        while (--_entitiesLeft >= 0)
        {// a okay
            _currentEntity = ref Unsafe.Add(ref _currentEntity, 1);
            if(_checkSparse)
            {
                
            }
            else
            {
                return true;
            }
        }

        while (--_archetypesLeft >= 0)
        {
            _current = ref Unsafe.Add(ref _current, 1);
            _entitiesLeft = _current.EntityCount - 1;

            if (_entitiesLeft < 0)
                continue;

            while (_checkSparse)
            {

            }

            _currentEntity = ref _current.GetEntityDataReference();
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the enumerator over a query.
    /// </summary>
    public EntityQueryEnumerator GetEnumerator() => this;
}
#endif