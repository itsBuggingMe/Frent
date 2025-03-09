using Frent.Core;
using Frent.Updating.Runners;

namespace Frent.Marshalling;

public static class WorldMarshal
{
    public static ref T GetComponent<T>(World world, Entity entity)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entity.EntityID);
        return ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(location.Archetype.Components.UnsafeArrayIndex(location.Archetype.GetComponentIndex<T>()))[location.Index];
    }

    public static Span<T> GetRawBuffer<T>(World world, Entity entity)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entity.EntityID);
        return UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(location.Archetype.Components.UnsafeArrayIndex(location.Archetype.GetComponentIndex<T>())).AsSpan();
    }

    public static ref T Get<T>(World world, int entityID)
    {

        EntityLookup location = world.EntityTable.UnsafeIndexNoResize(entityID);

        Archetype archetype = location.Location.Archetype;

        int compIndex = archetype.GetComponentIndex<T>();

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }
}
