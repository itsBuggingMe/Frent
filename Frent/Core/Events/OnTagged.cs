namespace Frent.Core.Events;
internal struct OnTagged
{
    public event Action<Entity, TagID> Tagged;
    internal void Invoke(Entity e, TagID c) => Tagged(e, c);
}
