using Frent.Core.Structures;

namespace Frent.Marshalling;

/// <summary>
/// Unsafe methods to write even faster code! Users are expected to know what they are doing and improper usage can result in corrupting world state and segfaults.
/// </summary>
/// <remarks>The APIs in this class are less stable, as many depend on implementation details.</remarks>
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
