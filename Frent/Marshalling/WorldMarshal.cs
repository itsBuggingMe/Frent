using Frent.Core;
using Frent.Updating.Runners;

namespace Frent.Marshalling;

public static class WorldMarshal
{
    public static ref T GetComponent<T>(World world, Entity entity)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entity.EntityID).Location;
        return ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(location.Archetype.Components.UnsafeArrayIndex(location.Archetype.GetComponentIndex<T>()))[location.Index];
    }

    public static Span<T> GetRawBuffer<T>(World world, Entity entity)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entity.EntityID).Location;
        return UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(location.Archetype.Components.UnsafeArrayIndex(location.Archetype.GetComponentIndex<T>())).AsSpan();
    }
}
