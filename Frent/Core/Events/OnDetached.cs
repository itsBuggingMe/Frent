namespace Frent.Core.Events;
internal struct OnDetached
{
    public event Action<Entity, TagID> Detached;
    internal void Invoke(Entity e, TagID c) => Detached(e, c);
}
