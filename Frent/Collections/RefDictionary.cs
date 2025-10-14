using Frent.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Frent.Collections;

//[DebuggerTypeProxy(typeof(RefDictionary<,>.RefDictionaryDebugView))]
internal class RefDictionary<TKey, TValue> where TKey : notnull
{
    public RefDictionary()
    {
        foreach (ref var entry in _entries.AsSpan())
        {
            entry.NextIndex = -2;
            entry.BucketIndex = -1;
        }
    }

    private Entry[] _entries = new Entry[4];
    private int _next;
    private int _free = -1;

    [StructLayout(LayoutKind.Auto)]
    internal struct Entry
    {
        /*_buckets*/
        // entry[mod(hash)]
        internal int BucketIndex;
        /*entries*/
        internal TKey Key;
        internal TValue Value;
        internal int NextIndex;
    }

    public bool ContainsKey(TKey key)
    {
        ref Entry entry = ref FindEntry(key);
        return !Unsafe.IsNullRef(ref entry);
    }

    public TValue? GetValueOrDefault(TKey key)
    {
        ref Entry entry = ref FindEntry(key);

        if (Unsafe.IsNullRef(ref entry))
        {
            return default;
        }

        return entry.Value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        ref Entry entry = ref FindEntry(key);

        if (Unsafe.IsNullRef(ref entry))
        {
            Unsafe.SkipInit(out value);
            MemoryHelpers.Poison(ref value);
            return false;
        }

        value = entry.Value;
        return true;
    }

    public bool Remove(TKey key) => Remove(key, out _);

    public bool Remove(TKey key, out TValue value)
    {
        Entry[] entries = _entries;
#if !NETSTANDARD
        Debug.Assert(BitOperations.PopCount((uint)entries.Length) == 1);
#endif

        int bucket = key.GetHashCode() & (entries.Length - 1);
        ref int connectedFrom = ref entries[bucket].BucketIndex;

        for (int next = connectedFrom; next >= 0;)
        {
            ref Entry current = ref entries[next];
            if (current.Key.Equals(key))
            {
                // relink linked list
                // say that 3 times fast
                connectedFrom = current.NextIndex;

                // add to free list
                current.NextIndex = _free;
                _free = next;
                value = current.Value;
                return true;
            }

            connectedFrom = ref current.NextIndex;
            next = connectedFrom;
        }
        
        Unsafe.SkipInit(out value);
        MemoryHelpers.Poison(ref value);
        return false;
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

        int bucket = key.GetHashCode() & (entries.Length - 1);

        for (int next = entries[bucket].BucketIndex; next >= 0;)
        {
            ref Entry current = ref entries[next];

            if (current.Key.Equals(key))
            {
                return ref current;
            }

            next = current.NextIndex;
        }

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
        _entries = new Entry[_entries.Length * 2];
        Entry[] entries = _entries;

        for(int i = 0; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];
            entry.NextIndex = -2;
            entry.BucketIndex = -1;
        }

        _next = 0;

        // rebuild
        for (int i = 0; i < oldEntries.Length; i++)
        {
            ref Entry entry = ref oldEntries[i];
            CreateEntry(entry.Key).Value = entry.Value;
        }

        return ref CreateEntry(key);
    }

    public Enumerator GetEnumerator() => new Enumerator(_entries, _next);

    internal struct Enumerator
    {
        internal Enumerator(Entry[] entries, int count)
        {
            _entries = entries;
            _count = count;
            _index = -1;
        }

        private int _index;
        private readonly int _count;
        private Entry[] _entries;
        private KeyValuePair<TKey, TValue> _current;
        public KeyValuePair<TKey, TValue> Current => _current;

        public bool MoveNext()
        {
            for(_index++; _index < _count; _index++)
            {
                ref Entry entry = ref _entries[_index];
                if (entry.NextIndex != -2)
                {
                    _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                    return true;
                }
            }


            return false;
        }
    }

    /*
     // save on some il
    private class RefDictionaryDebugView(RefDictionary<TKey, TValue> dict)
    {
        public IEnumerable<int> Free
        {
            get
            {
                for(int next = dict._free; next != -1; next = dict._entries[next].NextIndex)
                    yield return next;
            }
        }

        public IEnumerable<IEnumerable<Entry>> Buckets
        {
            get
            {
                for (int hash = 0; hash < dict._entries.Length; hash++)
                {
                    List<Entry> entryList = [];
                    for (int next = dict._entries[hash].BucketIndex; next != -1; next = dict._entries[next].NextIndex)
                    {
                        entryList.Add(dict._entries[next]);
                    }
                    yield return entryList;
                }
            }
        }
    }*/
}