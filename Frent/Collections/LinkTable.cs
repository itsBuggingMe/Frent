using Frent.Core;
using Frent.Core.Archetypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

// world link id -> some collection of outgoing & incoming links
internal struct LinkTable
{

#if NETSTANDARD
    internal LinkTableEntry[] Outgoing;
    internal LinkTableEntry[] Incoming;
    public readonly LinkTableEntry[] GetLinkTable(int incoming) => incoming == 0 ? Outgoing : Incoming;
#else
    [UnscopedRef] internal ref LinkTableEntry[] Outgoing => ref GetLinkTable(0);
    [UnscopedRef] internal ref LinkTableEntry[] Incoming => ref GetLinkTable(1);
    [UnscopedRef] public ref LinkTableEntry[] GetLinkTable(int incoming) => ref ((Span<LinkTableEntry[]>)LinkTables).UnsafeSpanIndex(incoming);
    internal InlineArray2<LinkTableEntry[]> LinkTables;
#endif
    public LinkTable()
    {
        Outgoing = [];
        Incoming = [];
    }

    // incoming: 0 -> outgoing table, 1 -> incoming table

    public bool HasLinks(int worldLinkId, int incoming)
    {
        LinkTableEntry[] entries = GetLinkTable(incoming);

        if (!((uint)worldLinkId < (uint)entries.Length))
            return false;

        return entries[worldLinkId].Any;
    }

    /// <summary>
    /// Gets the archetype and row of every entity on the other side of <paramref name="location"/>'s links of kind <paramref name="linkID"/>.
    /// </summary>
    /// <remarks>The spans point directly into link storage; they are invalidated by any link or structual change.</remarks>
    internal static void GetLinkedSlots(World world, LinkID linkID, scoped ref EntityLocation location, int incoming, out Span<Archetype> archetypes, out Span<int> rows)
    {
        if (!location.HasFlag((EntityFlags)((ushort)EntityFlags.HasHadOutgoingLinks << incoming)))
            goto fail;

        LinkTable[] worldLinkTable = world.WorldLinkTable;
        if (!(linkID.RawValue < worldLinkTable.Length))
            goto fail;

        ref LinkTable table = ref worldLinkTable.UnsafeArrayIndex(linkID.RawValue);
        LinkTableEntry[] entries = table.GetLinkTable(incoming);

        int worldLinkId = location.Archetype.GetExistingLinkID(location.Index);

        if (worldLinkId == 0 || !((uint)worldLinkId < (uint)entries.Length))
            goto fail;

        LinkTableEntry.GetLinks(ref entries[worldLinkId], out archetypes, out rows);

        return;

    fail:
        archetypes = default;
        rows = default;
        return;
    }
}

// can be 0 (null), small (<= 2), or large (T[])
// A small collection of entity rows and their archetypes
internal struct LinkTableEntry
{
    // null | Archetype | LargeStorage
    private object? _root;

    // row | count
    private int _row;
    // unused when large :(
    private int _mapBack;
    private int _linkedWorldId;

    public readonly bool Any => _root is not null;
    public Archetype RootAsArchetype => UnsafeExtensions.UnsafeCast<Archetype>(_root!);
    public int SingleRow
    {
        get
        {
            Debug.Assert(_root?.GetType() == typeof(Archetype));
            return _row;
        }
    }
    public int SingleLinkedWorldID
    {
        get
        {
            Debug.Assert(_root?.GetType() == typeof(Archetype));
            return _linkedWorldId;
        }
    }

    private readonly int ElementCount => _root is null ? 0 : (_root is Archetype ? 1 : _row);

    public static void GetLinks(ref LinkTableEntry entry, out Span<Archetype> archetypes, out Span<int> rows)
    {
        if (entry._root is null)
        {
            archetypes = [];
            rows = [];
        }
        else if (entry._root is Archetype)
        {
#if !NETSTANDARD
            archetypes = MemoryMarshal.CreateSpan(ref Unsafe.As<object?, Archetype>(ref entry._root), 1);
            rows = MemoryMarshal.CreateSpan(ref entry._row, 1);
#else
            archetypes = new Archetype[] { UnsafeExtensions.UnsafeCast<Archetype>(entry._root) };
            rows = new int[] { entry._row };
#endif
        }
        else
        {
            LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(entry._root);

            archetypes = s.Archetypes.AsSpan(0, entry._row);
            rows = s.Rows.AsSpan(0, entry._row);
        }
    }

    public int AddLinkChecked(Archetype archetype, int row, int linkedWorldId, int mapBack)
    {
        if (_root is null)
        {
            SetSingle(archetype, row, linkedWorldId, mapBack);
            return 0;
        }

        LargeStorage s;
        if (_root is Archetype existing)
        {
            if (_linkedWorldId == linkedWorldId)
                return -1;
            s = UpgradeToLarge(existing);
        }
        else
        {
            s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root);
            int count = _row;
            for (int i = 0; i < count; i++)
                if (s.Follow[i].LinkedWorldID == linkedWorldId)
                    return -1;
        }
        return AppendLarge(s, archetype, row, linkedWorldId, mapBack);
    }

    public int AddLinkUnchecked(Archetype archetype, int row, int linkedWorldId, int mapBack)
    {
        if (_root is null)
        {
            SetSingle(archetype, row, linkedWorldId, mapBack);
            return 0;
        }

        LargeStorage s = _root is Archetype existing
            ? UpgradeToLarge(existing)
            : UnsafeExtensions.UnsafeCast<LargeStorage>(_root);
        return AppendLarge(s, archetype, row, linkedWorldId, mapBack);
    }

    public readonly bool TryGetIndexByLinkedWorldId(int linkedWorldId, out int index)
    {
        if (_root is null)
        {
            index = -1;
            return false;
        }

        if (_root is Archetype)
        {
            if (_linkedWorldId == linkedWorldId)
            {
                index = 0;
                return true;
            }
            index = -1;
            return false;
        }

        LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root);
        int count = _row;
        LinkFollowData[] follow = s.Follow;
        for (int i = 0; i < count; i++)
        {
            if (follow.UnsafeArrayIndex(i).LinkedWorldID == linkedWorldId)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    public readonly int GetMapBack(int index) => _root is Archetype
        ? _mapBack
        : UnsafeExtensions.UnsafeCast<LargeStorage>(_root!).Follow[index].MapBack;

    public void SetMapBack(int index, int value)
    {
        if (_root is Archetype)
            _mapBack = value;
        else
            UnsafeExtensions.UnsafeCast<LargeStorage>(_root!).Follow[index].MapBack = value;
    }

    public void SetLocation(int index, Archetype archetype, int row)
    {
        if (_root is Archetype)
        {
            _root = archetype;
            _row = row;
        }
        else
        {
            LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root!);
            s.Archetypes.UnsafeArrayIndex(index) = archetype;
            s.Rows.UnsafeArrayIndex(index) = row;
        }
    }

    public void RemoveAt(int index, LinkTableEntry[] oppArray)
    {
        if (_root is Archetype)
        {
            // single element (index is 0); nothing to move
            _root = null;
            return;
        }

        LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root!);
        int last = _row - 1;

        if (index != last)
        {
            LinkFollowData movedFollow = s.Follow.UnsafeArrayIndex(last);
            s.Archetypes.UnsafeArrayIndex(index) = s.Archetypes.UnsafeArrayIndex(last);
            s.Rows.UnsafeArrayIndex(index) = s.Rows.UnsafeArrayIndex(last);
            s.Follow.UnsafeArrayIndex(index) = movedFollow;

            oppArray.UnsafeArrayIndex(movedFollow.LinkedWorldID).SetMapBack(movedFollow.MapBack, index);
        }

        s.Archetypes.UnsafeArrayIndex(last) = null!;
        _row = last;

        if (last == 0)
            _root = null;
    }

    public readonly void UpdateMirrors(LinkTableEntry[] oppArray, Archetype newArchetype, int newRow)
    {
        if (_root is null)
            return;

        if (_root is Archetype)
        {
            oppArray.UnsafeArrayIndex(_linkedWorldId).SetLocation(_mapBack, newArchetype, newRow);
            return;
        }

        LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root);
        int count = _row;
        for (int i = 0; i < count; i++)
        {
            ref LinkFollowData f = ref s.Follow.UnsafeArrayIndex(i);
            oppArray.UnsafeArrayIndex(f.LinkedWorldID).SetLocation(f.MapBack, newArchetype, newRow);
        }
    }

    public readonly void RemoveAllOpposing(LinkTableEntry[] oppArray, LinkTableEntry[] selfDirArray)
    {
        if (_root is null)
            return;

        if (_root is Archetype)
        {
            oppArray.UnsafeArrayIndex(_linkedWorldId).RemoveAt(_mapBack, selfDirArray);
            return;
        }

        LargeStorage s = UnsafeExtensions.UnsafeCast<LargeStorage>(_root);
        int count = _row;
        for (int i = 0; i < count; i++)
        {
            ref LinkFollowData f = ref s.Follow.UnsafeArrayIndex(i);
            oppArray.UnsafeArrayIndex(f.LinkedWorldID).RemoveAt(f.MapBack, selfDirArray);
        }
    }

    public void Clear() => _root = null;

    private void SetSingle(Archetype archetype, int row, int linkedWorldId, int mapBack)
    {
        _root = archetype;
        _row = row;
        _mapBack = mapBack;
        _linkedWorldId = linkedWorldId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private LargeStorage UpgradeToLarge(Archetype existing)
    {
        LargeStorage s = new();

        s.Archetypes.UnsafeArrayIndex(0) = existing;
        s.Rows.UnsafeArrayIndex(0) = _row;
        s.Follow.UnsafeArrayIndex(0) = new LinkFollowData { MapBack = _mapBack, LinkedWorldID = _linkedWorldId };

        _root = s;
        _row = 1; // now a count
        return s;
    }

    private int AppendLarge(LargeStorage s, Archetype archetype, int row, int linkedWorldId, int mapBack)
    {
        ref int nextIndex = ref _row;
        if (nextIndex == s.Archetypes.Length)
        {
            int newSize = nextIndex * 2;
            Array.Resize(ref s.Archetypes, newSize);
            Array.Resize(ref s.Rows, newSize);
            Array.Resize(ref s.Follow, newSize);
        }

        s.Archetypes.UnsafeArrayIndex(nextIndex) = archetype;
        s.Rows.UnsafeArrayIndex(nextIndex) = row;
        s.Follow.UnsafeArrayIndex(nextIndex) = new LinkFollowData { MapBack = mapBack, LinkedWorldID = linkedWorldId };
        return nextIndex++;
    }

    private sealed class LargeStorage
    {
        internal Archetype[] Archetypes = new Archetype[4];
        internal int[] Rows = new int[4];
        internal LinkFollowData[] Follow = new LinkFollowData[4];
    }

    private struct LinkFollowData
    {
        public int MapBack;
        public int LinkedWorldID;
    }
}
