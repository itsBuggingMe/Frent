using Frent.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Collections;

namespace Frent;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct EntityLocation
{
    //128 bits
    // When entity is dead:
    //  Archetype is null.
    //  Version is the next version to use.
    //  Index -1 if end of linked list, n where EntityTable[n] is the next free node.  
    //  Flags undefined.
    internal Archetype Archetype;
    internal int Index;
    internal EntityFlags Flags;
    internal ushort Version;

    internal readonly ArchetypeID ArchetypeID => Archetype.ID;

    public EntityLocation(Archetype archetype, int index)
    {
        Archetype = archetype;
        Index = index;
        //Flags = EntityFlags.None;
    }

    public EntityLocation(Archetype archetype, int index, EntityFlags flags)
    {
        Archetype = archetype;
        Index = index;
        Flags = flags;
    }

    public static EntityLocation Default { get; } = new EntityLocation(null!, int.MaxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasFlag(EntityFlags entityFlags)
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

    /// <remarks>
    /// May resize the internal bitset buffer
    /// </remarks>
    public ref Bitset GetBitset() => ref Archetype.GetBitset(Index);
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

    WorldCreate = 1 << 7,

    Events = Tagged | Detach | AddComp | RemoveComp | OnDelete | WorldCreate,

    HasSparseComponents = 1 << 8,

    HasWorldCommandBufferRemove = 1 << 9,
    HasWorldCommandBufferAdd = 1 << 10,
    HasWorldCommandBufferDelete = 1 << 11,

}