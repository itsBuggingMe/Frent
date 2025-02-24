using Frent.Core.Structures;

namespace Frent.Marshalling;

public static class EntityMarshal
{
    public static World GetWorld(Entity entity)
    {
        return GlobalWorldTables.Worlds.UnsafeIndexNoResize(entity.EntityID);
    }
}
