using Frent.Core.Structures;

namespace Frent.Marshalling;

/// <summary>
/// Contains unsafe methods for higher preformance operations.
/// </summary>
/// <remarks>API consumers are expected to understand the internals. Improper unsage could result in world corruption, memory corruption, or segmentation faults.<remarks/>
public static class EntityMarshal
{
    public static World? GetWorld(Entity entity)
    {
        return GlobalWorldTables.Worlds.UnsafeIndexNoResize(entity.EntityID);
    }
    public static int EntityID(Entity entity)
    {
        return entity.EntityID;
    }
}
