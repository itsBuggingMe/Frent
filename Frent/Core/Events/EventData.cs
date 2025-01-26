namespace Frent.Core.Events;

//8*6 = 48 bytes
internal struct EventData
{
    public Action<Entity, ComponentID> ComponentAdded;
    public MulticastGenericAction<Entity>? GenericComponentAdded;

    public Action<Entity, ComponentID> ComponentRemoved;
    public MulticastGenericAction<Entity>? GenericComponentRemoved;

    public event Action<Entity, TagID> Tagged;
    public event Action<Entity, TagID> Detached;
}
