using Frent.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EntityLocation
{
    //TODO: i dont think this struct is packed properly!
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

    AddGenericComp = 1 << 3,
    RemoveComp = 1 << 4,

    RemoveGenericComp = 1 << 5,

    OnDelete = 1 << 6,
     
    Events = Tagged | Detach | AddComp | RemoveComp | OnDelete | WorldCreate,

    WorldCreate = 1 << 7,

    HasWorldCommandBufferRemove = 1 << 8,

    HasWorldCommandBufferAdd = 1 << 9,

    HasWorldCommandBufferDelete = 1 << 10,

    IsUnmergedEntity = 1 << 11,
}