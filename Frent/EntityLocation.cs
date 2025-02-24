using Frent.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation
{
    internal Archetype Archetype;
    internal int Index;
    internal EntityFlags Flags;


    internal ArchetypeID ArchetypeID => Archetype.ID;


    public EntityLocation(Archetype archetype, int index)
    {
        Archetype = archetype;
        Index = index;
        Flags = EntityFlags.None;
    }

    public EntityLocation(Archetype archetype, int index, EntityFlags flags)
    {
        Archetype = archetype;
        Index = index;
        Flags = flags;
    }

    public static EntityLocation Default { get; } = new EntityLocation(null!, int.MaxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasEvent(EntityFlags entityFlags)
    {
        var res = (Flags & entityFlags) != EntityFlags.None;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasEventFlag(EntityFlags entityFlags, EntityFlags target)
    {
        var res = (entityFlags & target) != EntityFlags.None;
        return res;
    }
}

[Flags]
internal enum EntityFlags : ushort
{
    None = 0,

    Tagged = 1 << 0,
    Detach = 1 << 1,

    AddComp = 1 << 2,
    RemoveComp = 1 << 3,

    OnDelete = 1 << 4,
     
    Events = Tagged | Detach | AddComp | RemoveComp | OnDelete,

    WorldCreate = 1 << 5,

    WorldTagged = 1 << 6,
    WorldDetach = 1 << 7,

    WorldAddComp = 1 << 8,
    WorldRemoveComp = 1 << 9,

    WorldOnDelete = 1 << 10,

    WorldEvents = WorldTagged | WorldDetach | WorldAddComp | WorldRemoveComp | WorldOnDelete,

    AllEvents = Events | WorldEvents,

    HasWorldCommandBufferRemove = 1 << 11,

    HasWorldCommandBufferAdd = 1 << 13,

    HasWorldCommandBufferDelete = 1 << 14,

    IsUnmergedEntity = 1 << 15,
}