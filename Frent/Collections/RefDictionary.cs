using Frent.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Frent.Collections;
internal class RefDictionary<TKey, TValue> where TKey : notnull, IEquatable<TKey>
{
    public RefDictionary()
    {
        foreach (ref var entry in _entries.AsSpan())
        {
            entry.NextIndex = -1;
            entry.BucketIndex = -1;
        }
    }

    private Entry[] _entries = new Entry[4];
    private int _next;
    private int _free = -1;

    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        /*_buckets*/
        // entry[mod(hash)]
        internal int BucketIndex;
        /*entries*/
        internal TKey Key;
        internal TValue Value;
        internal int NextIndex;
    }

    public bool Remove(TKey key)
    {
        Entry[] entries = _entries;
#if !NETSTANDARD
        Debug.Assert(BitOperations.PopCount((uint)entries.Length) == 1);
#endif

        int index = key.GetHashCode() & entries.Length - 1;
        int previousIndex = -1;

        do
        {
            ref Entry current = ref entries[index];
            if (current.Key.Equals(key))
            {
                // add to free list
                current.NextIndex = _free;
                _free = index;
                // relink linked list
                // say that 3 times fast
                if(current.NextIndex == -1)
                {// already end of list

                }
                else
                {

                }
                return true;
            }

            previousIndex = index;
            index = current.NextIndex;
        } while (index >= 0);

        Debug.Assert(index == -1);
        return true;
    }

    public ref TValue? GetValueRefOrAddDefault(TKey key, out bool exists)
    {
        ref Entry entry = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref entry))
        {
            exists = false;
            return ref CreateEntry(key).Value!;
        }
        exists = true;
        return ref entry.Value!;
    }

    public ref TValue GetValueRefOrNullRef(TKey key)
    {
        ref Entry entry = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref entry))
            return ref Unsafe.NullRef<TValue>();
        return ref entry.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Entry FindEntry(TKey key)
    {
        Entry[] entries = _entries;
#if !NETSTANDARD
        Debug.Assert(BitOperations.PopCount((uint)entries.Length) == 1);
#endif

        int index = key.GetHashCode() & entries.Length - 1;

        do
        {
            ref Entry current = ref entries[index];
            if (current.Key.Equals(key))
            {
                return ref current;
            }

            index = current.NextIndex;
        } while (index >= 0);

        Debug.Assert(index == -1);
        return ref Unsafe.NullRef<Entry>();
    }

    private ref Entry CreateEntry(TKey key)
    {
        Entry[] entries = _entries;
        int next;
        if (_free == -1)
        {
            next = _next++;
            if (!((uint)next < (uint)entries.Length))
            {
                return ref DoubleAndCreateEntry(key);
            }
        }
        else
        {
            next = _free;
            _free = entries[next].NextIndex;
        }

        ref Entry bucket = ref entries[key.GetHashCode() & (entries.Length - 1)];
        int oldBucketIndex = bucket.BucketIndex;
        // hash -> bucket (next entry)
        bucket.BucketIndex = next;
        ref Entry nextEntry = ref entries[next];

        nextEntry.Key = key;
        // next entry -> old entry index
        nextEntry.NextIndex = oldBucketIndex;

        // newEntry.Value initalized by caller
        return ref nextEntry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref Entry DoubleAndCreateEntry(TKey key)
    {
        Entry[] oldEntries = _entries;
        Array.Resize(ref _entries, _entries.Length * 2);
        Entry[] entries = _entries;

        for(int i = entries.Length >> 1; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];
            entry.NextIndex = -1;
            entry.BucketIndex = -1;
        }

        // rebuild
        int modMask = entries.Length - 1;
        for (int i = 0; i < oldEntries.Length; i++)
        {
            ref Entry entry = ref oldEntries[i];
            int bucketIndex = entry.Key.GetHashCode() & modMask;

            CreateEntry(entry.Key).Value = entry.Value;
        }

        return ref CreateEntry(key);
    }
}