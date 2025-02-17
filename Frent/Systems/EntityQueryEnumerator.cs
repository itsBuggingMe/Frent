using Frent.Core;
using Frent.Variadic.Generator;

namespace Frent.Systems;

[Variadic("    private Span<T> _currentSpan1;", "|    private Span<T$> _currentSpan$;\n|")]
[Variadic("        Item1 = new Ref<T>(ref _currentSpan1[_componentIndex]),", 
    "|        Item$ = new Ref<T$>(ref _currentSpan$[_componentIndex]),\n|")]
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
    private EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        _archetypeIndex = -1;
    }

    public EntityRefTuple<T> Current => new()
    {
        Entity = _entityIds[_componentIndex].ToEntity(_world),
        Item1 = new Ref<T>(ref _currentSpan1[_componentIndex]),
    };

    public void Dispose()
    {
        _world.ExitDisallowState();
    }

    public bool MoveNext()
    {
        if(++_componentIndex < _currentSpan1.Length)
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

    public struct QueryEnumerable(Query query)
    {
        public EntityQueryEnumerator<T> GetEnumerator() => new EntityQueryEnumerator<T>(query);
    }
}

public ref struct EntityQueryEnumerator
{
    private int _archetypeIndex;
    private int _componentIndex;
    private World _world;
    private Span<Archetype> _archetypes;
    private Span<EntityIDOnly> _entityIds;
    private EntityQueryEnumerator(Query query)
    {
        _world = query.World;
        _world.EnterDisallowState();
        _archetypes = query.AsSpan();
        _archetypeIndex = -1;
    }

    public Entity Current => _entityIds[_componentIndex].ToEntity(_world);

    public void Dispose()
    {
        _world.ExitDisallowState();
    }

    public bool MoveNext()
    {
        if (++_componentIndex < _entityIds.Length)
        {
            return true;
        }

        _componentIndex = 0;
        _archetypeIndex++;

        if ((uint)_archetypeIndex < (uint)_archetypes.Length)
        {
            var cur = _archetypes[_archetypeIndex];
            _entityIds = cur.GetEntitySpan();
            return true;
        }

        return false;
    }

    public struct QueryEnumerable(Query query)
    {
        public EntityQueryEnumerator GetEnumerator() => new EntityQueryEnumerator(query);
    }
}