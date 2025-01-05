using Frent.Core;

namespace Frent;

internal struct EntityLocation(Archetype archetype, ushort chunkIndex, ushort componentIndex)
{
    internal Archetype Archetype = archetype;
    internal ushort ChunkIndex = chunkIndex;
    internal ushort ComponentIndex = componentIndex;
    public static EntityLocation Default { get; } = new EntityLocation(null!, ushort.MaxValue, ushort.MaxValue);
}
