using Frent.Core;

namespace Frent.Systems;

/// <summary>
/// Enumerator over a set of <see cref="Entity"/> instances.
/// </summary>
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

    /// <summary>
    /// Moves to the next <see cref="Entity"/> instance.
    /// </summary>
    /// <returns><see langword="true"/> when its possible to enumerate further, otherwise <see langword="false"/>.</returns>
    public bool MoveNext() => ++_index < _entities.Length;

    /// <summary>
    /// The current <see cref="Entity"/> instance.
    /// </summary>
    public Entity Current => _entities[_index].ToEntity(_world);

    /// <summary>
    /// Proxy struct used to get an <see cref="EntityEnumerator"/>.
    /// </summary>
    public ref struct EntityEnumerable
    {
        private World _world;
        private Span<EntityIDOnly> _entities;
        internal EntityEnumerable(World world, Span<EntityIDOnly> entities)
        {
            _world = world;
            _entities = entities;
        }

        /// <summary>
        /// Gets an <see cref="EntityEnumerator"/> from this <see cref="EntityEnumerable"/>. Allows for the usage of foreach syntax.
        /// </summary>
        public EntityEnumerator GetEnumerator() => new EntityEnumerator(_world, _entities);
    }
}