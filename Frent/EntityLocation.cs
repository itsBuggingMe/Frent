using Frent.Core;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation(ArchetypeID archetype, byte chunkIndex, ushort componentIndex)
{
    internal ArchetypeID ArchetypeID = archetype;
    internal ushort ComponentIndex = componentIndex;
    internal byte ChunkIndex = chunkIndex;
    internal EntityFlags Flags = EntityFlags.None;
    public static EntityLocation Default { get; } = new EntityLocation(new(ushort.MaxValue), byte.MaxValue, ushort.MaxValue);

    public Archetype Archetype(World world)
    {
        return world.WorldArchetypeTable[ArchetypeID.ID];
    }
}

[Flags]
internal enum EntityFlags : byte
{
    None = 0,
    Tagged = 1 << 0,
    Detach = 1 << 1,
    AddComp = 1 << 2,
    RemoveComp = 1 << 3,
    NeedsDispose = 1 << 4,
}