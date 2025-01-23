namespace Frent.Core.Events;

public struct OnComponentRemoved
{
    public event Action<Entity, ComponentID> ComponentRemoved;
    public MulticastGenericAction<Entity>? GenericComponentRemoved { get; set; }
    internal void Invoke(Entity e, ComponentID c) => ComponentRemoved(e, c);
}
