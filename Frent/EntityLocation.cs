using Frent.Core;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation(ArchetypeID archetype, ushort chunkIndex, ushort componentIndex)
{
    internal ArchetypeID ArchetypeID = archetype;
    internal ushort ComponentIndex = componentIndex;
    internal ushort ChunkIndex = chunkIndex;
    internal EntityFlags Flags = EntityFlags.None;

    public static EntityLocation Default { get; } = new EntityLocation(new(ushort.MaxValue), ushort.MaxValue, ushort.MaxValue);

    public readonly bool HasEvent(EntityFlags entityFlags)
    {
        var res =  (Flags & entityFlags) != EntityFlags.None;
        return res;
    }

    public Archetype Archetype(World world)
    {
        return world.WorldArchetypeTable[ArchetypeID.ID];
    }
}

[Flags]
internal enum EntityFlags : ushort
{
    None = 0,

    Tagged = 1 << 0,
    Detach = 1 << 1,

    AddComp = 1 << 2,
    GenericAddComp = 1 << 3,

    RemoveComp = 1 << 4,
    GenericRemoveComp = 1 << 5,

    OnDelete = 1 << 6,

    Events = Tagged | Detach | AddComp | RemoveComp | GenericAddComp | GenericAddComp | OnDelete,
    
    //NeedsDispose = 1 << 4,
}