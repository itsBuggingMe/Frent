using Frent.Core;

namespace Frent.Systems;
public ref struct EntityEnumerator
{
    private World _world;
    private Span<EntityIDOnly> _entities;
    private int _index;
    internal EntityEnumerator(World world, Span<EntityIDOnly> entities)
    {
        _world = world;
        _entities = entities;
        _index = -1;
    }

    public bool MoveNext() => ++_index < _entities.Length;
    public Entity Current => _entities[_index].ToEntity(_world);

    public ref struct EntityEnumerable
    {
        private World _world;
        private Span<EntityIDOnly> _entities;
        internal EntityEnumerable(World world, Span<EntityIDOnly> entities)
        {
            _world = world;
            _entities = entities;
        }

        public EntityEnumerator GetEnumerator() => new EntityEnumerator(_world, _entities);
    }
}