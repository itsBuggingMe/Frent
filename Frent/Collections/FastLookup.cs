using Frent.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Frent.Collections;

internal struct FastLookup()
{
    private LookupData _data;
    private int index;
    internal Archetype[] Archetypes = new Archetype[8];
    internal Dictionary<uint, Archetype> FallbackLookup = [];

    public uint GetKey(ushort id, ArchetypeID archetypeID)
    {
        uint key = ((uint)id << 16) | archetypeID.ID;
        return key;
    }

    public Archetype? TryGetValue(ushort id, ArchetypeID archetypeID)
    {
        uint key = ((uint)id << 16) | archetypeID.ID;
        int index = LookupIndex(key);
        if(index != 32)
        {
            return Archetypes.UnsafeArrayIndex(index);
        }

        if(FallbackLookup.TryGetValue(key, out Archetype? value))
        {
            return value;
        }

        return null;
    }

    public void SetArchetype(ushort id, ArchetypeID from, Archetype to)
    {
        uint key = ((uint)id << 16) | from.ID;
        
        FallbackLookup[key] = to;

        LookupData.Index(ref _data, index) = key;
        Archetypes[index] = to;

        index = (index + 1) & 7;
    }

    public int LookupIndex(uint key)
    {
#if NET7_0_OR_GREATER
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<uint> bits = Vector256.Equals(Vector256.Create(key), Vector256.LoadUnsafe(ref _data.l0));
            int index = BitOperations.TrailingZeroCount(bits.ExtractMostSignificantBits());
            return index;
        }
        //else if (Vector128.IsHardwareAccelerated)
        //{
        //    Vector128<uint> lower = Vector128.Equals(Vector128.Create(key), Vector128.LoadUnsafe(ref l0));
        //    Vector128<uint> upper = Vector128.Equals(Vector128.Create(key), Vector128.LoadUnsafe(ref l4));
        //
        //    uint lowerMask = lower.ExtractMostSignificantBits();
        //    uint upperMask = upper.ExtractMostSignificantBits() << 4;
        //
        //    int index = BitOperations.TrailingZeroCount(lowerMask | upperMask);
        //    return index;
        //}
#endif
        int bclIndex = MemoryMarshal.CreateSpan(ref _data.l0, 8).IndexOf(key);
        return bclIndex == -1 ? 32 : bclIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct LookupData
    {
        public static ref uint Index(ref LookupData data, int index)
        {
#if DEBUG
            if(index< 0 || index >= 8)
                throw new IndexOutOfRangeException();
#endif

            return ref Unsafe.Add(ref data.l0, index);
        }

        internal uint l0;
        internal uint l1;
        internal uint l2;
        internal uint l3;
        internal uint l4;
        internal uint l5;
        internal uint l6;
        internal uint l7;
    }
}