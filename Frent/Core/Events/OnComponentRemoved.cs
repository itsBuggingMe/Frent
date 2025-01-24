namespace Frent.Core.Events;

internal struct OnComponentRemoved
{
    public event Action<Entity, ComponentID> ComponentRemoved;
    public MulticastGenericAction<Entity>? GenericComponentRemoved { get; set; }
    internal void Invoke(Entity e, ComponentID c) => ComponentRemoved(e, c);
}
