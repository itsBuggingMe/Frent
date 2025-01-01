using Frent.Core;

namespace Frent;

public struct EntityLocation(Archetype archetype, ushort chunkIndex, ushort componentIndex)
{
    internal Archetype Archetype = archetype;
    internal ushort ChunkIndex = chunkIndex;
    internal ushort ComponentIndex = componentIndex;
}
