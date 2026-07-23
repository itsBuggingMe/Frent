using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Systems;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

// world link id -> some collection of outgoing & incoming links
internal struct LinkTable()
{
    internal LinkTableEntry[] OutgoingLinks = [];
    internal LinkTableEntry[] IncomingLinks = [];

    public readonly bool HasAnyLinksIncoming(int worldLinkId)
        => HasAnyLinksCore(IncomingLinks, worldLinkId);

    public readonly bool HasAnyLinksOutgoing(int worldLinkId)
        => HasAnyLinksCore(OutgoingLinks, worldLinkId);

    private static bool HasAnyLinksCore(LinkTableEntry[] links, int worldLinkId)
    {
        if (!((uint)worldLinkId < (uint)links.Length))
            return false;
        return links[worldLinkId].ArchetypesOrArrays[0] is null;
    }
}

// can be 0 (null), small (<= 2), or large (T[])
// A small collection of entity ids and their archetypes
internal struct LinkTableEntry
{
    // can be some number of Archetypes or Archetype[], int[]
    public InlineArray2<object?> ArchetypesOrArrays;
    // corresponding entity ids if ArchetypesOrArrays is Archetypes, Length otherwise
    // CountOrEntityIDs[0] is count of entities in the arrays above
    // CountOrEntityIDs[1] is used as a bloom filter where entity id is directly used as a fast path for error checking
    public InlineArray2<int> CountOrEntityIDs;


    /// <summary>
    /// True if there is not an existing duplicate, false if nothing changed since there was a dupe
    /// </summary>
    public bool AddLink(Archetype archetype, int target)
    {
        if (ArchetypesOrArrays[0] is null)
        {
            ArchetypesOrArrays[0] = archetype;
            CountOrEntityIDs[0] = target;
            return true;
        }
        if (ArchetypesOrArrays[1] is null)
        {
            if (CountOrEntityIDs[0] == target)
                return false;

            ArchetypesOrArrays[1] = archetype;
            CountOrEntityIDs[1] = target;
            return true;
        }

        if (ArchetypesOrArrays[0] is Archetype)
        {
            UpgradeStorage();
        }

        return AddLinkLarge(archetype, target);
    }

    // unsafe!!! only call if known current storage mode is large
    private bool AddLinkLarge(Archetype archetype, int target)
    {
        int bloomBit = 1 << (target & 31);
        if ((CountOrEntityIDs[1] & bloomBit) != 0 && StorageContainsEntity(target, UnsafeExtensions.UnsafeCast<int[]>(ArchetypesOrArrays[1]!)))
            return false;

        int index = CountOrEntityIDs[0];
        MemoryHelpers.GetValueOrResize(ref Unsafe.As<object, Archetype[]>(ref ArchetypesOrArrays[0]!), index)
            = archetype;
        MemoryHelpers.GetValueOrResize(ref Unsafe.As<object, int[]>(ref ArchetypesOrArrays[1]!), index)
            = target;
        CountOrEntityIDs[0]++;

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpgradeStorage()
    {
        Archetype archetype0 = UnsafeExtensions.UnsafeCast<Archetype>(ArchetypesOrArrays[0]!);
        Archetype archetype1 = UnsafeExtensions.UnsafeCast<Archetype>(ArchetypesOrArrays[1]!);
        int id0 = CountOrEntityIDs[0];
        int id1 = CountOrEntityIDs[1];

        Archetype[] archetypes = [archetype0, archetype1, null!, null!];
        int[] entityIds = [id0, id0, default, default];

        ArchetypesOrArrays[0] = archetypes;
        ArchetypesOrArrays[1] = entityIds;

        CountOrEntityIDs[0] = 2;
        CountOrEntityIDs[1] = (1 << (id0 & 31)) | (1 << (id1 & 31));
    }

    private static bool StorageContainsEntity(int entityId, int[] ids)
    {
        // TODO: improve case with many many ids
        return Array.IndexOf(ids, entityId) != -1;
    }
}