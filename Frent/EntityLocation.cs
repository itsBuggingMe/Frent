using Frent.Core;

namespace Frent;

public struct EntityLocation
{
    public EntityLocation(Archetype archetype, ushort chunkIndex, ushort componentIndex)
    {
        Archetype = archetype;
        ChunkIndex = chunkIndex;
        ComponentIndex = componentIndex;
    }

    internal Archetype Archetype;
    internal ushort ChunkIndex;
    internal ushort ComponentIndex;
}
