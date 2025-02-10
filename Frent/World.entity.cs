using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating.Runners;

namespace Frent;

partial class World
{
    public ref T Get<T>(Entity entity)
    {
        if (entity.PackedWorldInfo != PackedIDVersion)
            FrentExceptions.Throw_InvalidOperationException("Entity does not belong to this world");

        ref EntityLookup entityLookup = ref EntityTable.UnsafeIndexNoResize(entity.EntityID);
        if (entityLookup.Version != entity.EntityVersion)
            FrentExceptions.Throw_InvalidOperationException(Entity.EntityIsDeadMessage);

        Archetype archetype = entityLookup.Location.Archetype(this);
        int componentID = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        if (componentID >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException<T>();

        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(componentID));
        return ref storage[entityLookup.Location.Index];
    }
}
