using Frent.Core;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation(ArchetypeID archetype, ushort chunkIndex, ushort componentIndex)
{
    internal ArchetypeID ArchetypeID = archetype;
    internal ushort ChunkIndex = chunkIndex;
    internal ushort ComponentIndex = componentIndex;
    public static EntityLocation Default { get; } = new EntityLocation(new(ushort.MaxValue), ushort.MaxValue, ushort.MaxValue);

    public Archetype Archetype(World world)
    {
        return world.WorldArchetypeTable[ArchetypeID.ID];
    }
}
