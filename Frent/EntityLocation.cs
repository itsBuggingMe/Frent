using Frent.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation
{
    internal ArchetypeID ArchetypeID;
    internal int Index;
    internal EntityFlags Flags;

    public EntityLocation(ArchetypeID archetype, int index)
    {
        ArchetypeID = archetype;
        Index = index;
        Flags = EntityFlags.None;
    }

    public EntityLocation(ArchetypeID archetype, int index, EntityFlags flags)
    {
        ArchetypeID = archetype;
        Index = index;
        Flags = flags;
    }

    public static EntityLocation Default { get; } = new EntityLocation(new(ushort.MaxValue), ushort.MaxValue, ushort.MaxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasEvent(EntityFlags entityFlags)
    {
        var res = (Flags & entityFlags) != EntityFlags.None;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Archetype Archetype(World world)
    {
        return world.WorldArchetypeTable.UnsafeArrayIndex(ArchetypeID.ID);
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

    WorldCreate = 1 << 7,

    WorldTagged = 1 << 8,
    WorldDetach = 1 << 9,

    WorldAddComp = 1 << 10,
    WorldGenericAddComp = 1 << 11,

    WorldRemoveComp = 1 << 12,
    WorldGenericRemoveComp = 1 << 13,

    WorldOnDelete = 1 << 14,

    WorldEvents = WorldTagged | WorldDetach | WorldAddComp | WorldRemoveComp | WorldGenericAddComp | WorldGenericAddComp | WorldOnDelete,

    AllEvents = Events | WorldEvents,

    //NeedsDispose = 1 << 4,
}