namespace Frent.Core.Events;

public struct OnComponentAdded
{
    public event Action<Entity, ComponentID> ComponentAdded;
    public MulticastGenericAction<Entity>? GenericComponentAdded { get; set; }
}