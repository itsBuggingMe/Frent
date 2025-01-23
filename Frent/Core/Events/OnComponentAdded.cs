using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Security.AccessControl;
using System.Text;

namespace Frent.Core.Events;

public struct OnComponentAdded
{
    public event Action<Entity, ComponentID> ComponentAdded;
    public MulticastGenericAction<Entity>? GenericComponentAdded { get; set; }
    internal void Invoke(Entity e, ComponentID c) => ComponentAdded(e, c);

    internal static void TryInvokeAction(Archetype archetype, IComponentRunner runner, Entity entity, ComponentID componentID, ushort chunk, ushort component)
    {
        int eventIndex = GlobalWorldTables.ComponentIndex(archetype.ID, Component<AddComponent>.ID);
        var arr = archetype.Components;
        if (eventIndex <= arr.Length)
        {
            var @event = ((ComponentStorage<OnComponentAdded>)arr[eventIndex]).Chunks[chunk][component];
            @event.Invoke(entity, componentID);
            if (@event.GenericComponentAdded is not null)
                runner.InvokeGenericActionWith(@event.GenericComponentAdded, entity, chunk, component);
        }
    }
}