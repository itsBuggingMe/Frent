using Frent.Core;
using Frent.Core.Archetypes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

// world link id -> some collection of outgoing & incoming links
internal struct LinkTable()
{
    // allocating a LinkTable array does not run these initalizers - World fills every slot with new LinkTable()
    internal LinkTableEntry[] OutgoingLinks = [];
    internal LinkTableEntry[] IncomingLinks = [];

    /// <summary>
    /// Gets the archetype and row of every entity on the other side of <paramref name="location"/>'s links of kind <paramref name="linkID"/>.
    /// </summary>
    /// <remarks>The spans point directly into link storage; they are invalidated by any link or structual change.</remarks>
    internal static void GetLinkedSlots(World world, LinkID linkID, scoped ref EntityLocation location, bool incoming, out Span<Archetype> archetypes, out Span<int> rows)
    {
        archetypes = default;
        rows = default;

        if (!location.HasFlag(incoming ? EntityFlags.HasHadIncomingLinks : EntityFlags.HasHadOutgoingLinks))
            return;

        LinkTable[] worldLinkTable = world.WorldLinkTable;
        if (!((uint)linkID.RawValue < (uint)worldLinkTable.Length))
            return;

        ref LinkTable table = ref worldLinkTable.UnsafeArrayIndex(linkID.RawValue);
        LinkTableEntry[] entries = incoming ? table.IncomingLinks : table.OutgoingLinks;

        // 0 is the null world link id - the entity was never linked while at this archetype and row
        int worldLinkId = location.Archetype.GetExistingLinkID(location.Index);
        if (worldLinkId == 0 || !((uint)worldLinkId < (uint)entries.Length))
            return;

        LinkTableEntry.GetLinks(ref entries[worldLinkId], out archetypes, out rows);
    }
}

// can be 0 (null), small (<= 2), or large (T[])
// A small collection of entity rows and their archetypes
internal struct LinkTableEntry
{
    // can be some number of Archetypes or Archetype[], int[]
    public InlineArray2<object?> ArchetypesOrArrays;
    // corresponding entity rows if ArchetypesOrArrays is Archetypes, Length otherwise
    // CountOrEntityIDs[0] is count of entities in the arrays above
    // CountOrEntityIDs[1] is used as a bloom filter where entity rows is directly used as a fast path for error checking
    public InlineArray2<int> CountOrEntityRows;

    public static void GetLinks(ref LinkTableEntry entry, out Span<Archetype> archetypes, out Span<int> rows)
    {
        if (entry.ArchetypesOrArrays[0] is null)
        {
            archetypes = default;
            rows = default;
            return;
        }

        if (entry.ArchetypesOrArrays[0] is Archetype)
        {
            int inlineCount = entry.ArchetypesOrArrays[1] is null ? 1 : 2;
            archetypes = InlineArrayHelper.AsSpan(ref Unsafe.As<InlineArray2<object>, InlineArray2<Archetype>>(ref entry.ArchetypesOrArrays!))![..inlineCount];
            rows = InlineArrayHelper.AsSpan(ref entry.CountOrEntityRows)[..inlineCount];
            return;
        }

        int count = entry.CountOrEntityRows[0];
        archetypes = UnsafeExtensions.UnsafeCast<Archetype[]>(entry.ArchetypesOrArrays[0]!).AsSpan(0, count);
        rows = UnsafeExtensions.UnsafeCast<int[]>(entry.ArchetypesOrArrays[1]!).AsSpan(0, count);
    }

    public bool RemoveLink(Archetype archetype, int row)
    {
        if (ArchetypesOrArrays[0] is null)
            return false;

        if (ArchetypesOrArrays[1] is null)
        {
            if (CountOrEntityRows[0] == row && ArchetypesOrArrays[0] == archetype)
            {
                //CountOrEntityIDs[0] = default;
                ArchetypesOrArrays[0] = default;
                return true;
            }

            return false;
        }

        if (ArchetypesOrArrays[0] is Archetype)
        {
            return RemoveElement(archetype, row, InlineArrayHelper.AsSpan(ref Unsafe.As<InlineArray2<object>, InlineArray2<Archetype>>(ref ArchetypesOrArrays!))!, InlineArrayHelper.AsSpan(ref CountOrEntityRows));
        }

        int count = CountOrEntityRows[0];
        bool removed = RemoveElement(
            archetype,
            row,
            UnsafeExtensions.UnsafeCast<Archetype[]>(ArchetypesOrArrays[0]!).AsSpan(0, count),
            UnsafeExtensions.UnsafeCast<int[]>(ArchetypesOrArrays[1]!).AsSpan(0, count));


        if (removed)
            CountOrEntityRows[0]--;

        return removed;
    }

    private static bool RemoveElement(Archetype archetype, int row, Span<Archetype> archetypes, Span<int> rows)
    {
        int index = GetIndexOfRow(archetype, row, archetypes, rows);

        if (index == -1)
            return false;

        rows[index] = rows[^1];
        archetypes[index] = archetypes[^1];
        archetypes[^1] = default!;

        return true;
    }

    /// <summary>
    /// True if there is not an existing duplicate, false if nothing changed since there was a dupe
    /// </summary>
    public bool AddLink(Archetype archetype, int row)
    {
        if (ArchetypesOrArrays[0] is null)
        {
            ArchetypesOrArrays[0] = archetype;
            CountOrEntityRows[0] = row;
            return true;
        }
        if (ArchetypesOrArrays[1] is null)
        {
            if (CountOrEntityRows[0] == row && ArchetypesOrArrays[0] == archetype)
                return false;

            ArchetypesOrArrays[1] = archetype;
            CountOrEntityRows[1] = row;
            return true;
        }

        if (ArchetypesOrArrays[0] is Archetype)
        {
            UpgradeStorage();
        }

        return AddLinkLarge(archetype, row);
    }

    // unsafe!!! only call if known current storage mode is large
    private bool AddLinkLarge(Archetype archetype, int row)
    {
        int count = CountOrEntityRows[0];
        int bloomBit = BloomBit(archetype, row);
        if ((CountOrEntityRows[1] & bloomBit) != 0 &&
            GetIndexOfRow(archetype, row, UnsafeExtensions.UnsafeCast<Archetype[]>(ArchetypesOrArrays[0]!).AsSpan(0, count), UnsafeExtensions.UnsafeCast<int[]>(ArchetypesOrArrays[1]!).AsSpan(0, count)) != -1)
            return false;

        MemoryHelpers.GetValueOrResize(ref Unsafe.As<object, Archetype[]>(ref ArchetypesOrArrays[0]!), count)
            = archetype;
        MemoryHelpers.GetValueOrResize(ref Unsafe.As<object, int[]>(ref ArchetypesOrArrays[1]!), count)
            = row;
        CountOrEntityRows[0] = count + 1;
        CountOrEntityRows[1] |= bloomBit;

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpgradeStorage()
    {
        Archetype archetype0 = UnsafeExtensions.UnsafeCast<Archetype>(ArchetypesOrArrays[0]!);
        Archetype archetype1 = UnsafeExtensions.UnsafeCast<Archetype>(ArchetypesOrArrays[1]!);
        int row0 = CountOrEntityRows[0];
        int row1 = CountOrEntityRows[1];

        Archetype[] archetypes = [archetype0, archetype1, null!, null!];
        int[] entityRows = [row0, row1, default, default];

        ArchetypesOrArrays[0] = archetypes;
        ArchetypesOrArrays[1] = entityRows;

        CountOrEntityRows[0] = 2;
        CountOrEntityRows[1] = BloomBit(archetype0, row0) | BloomBit(archetype1, row1);
    }

    private static int BloomBit(Archetype archetype, int row) => 1 << ((row ^ archetype.GetHashCode()) & 31);

    private static int GetIndexOfRow(Archetype archetype, int row, Span<Archetype> archetypes, Span<int> rows)
    {
        int index = -1;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == row && archetypes[i] == archetype)
            {
                index = i;
                break;
            }
        }

        return index;
    }
}
