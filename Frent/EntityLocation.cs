using Frent.Core;

namespace Frent;
internal struct EntityLocation(Archetype archetype, ushort chunkIndex, ushort componentIndex)
{
    internal Archetype Archetype = archetype;
    internal ushort ChunkIndex = chunkIndex;
    internal ushort ComponentIndex = componentIndex;
}
