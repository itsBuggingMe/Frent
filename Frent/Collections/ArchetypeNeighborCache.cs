using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Frent.Collections;

//160 bits, 20 bytes
internal struct ArchetypeNeighborCache
{
    //128 bits
    private LookupIDs _keysAndValues;
    //32
    private int _nextIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Traverse(ushort value)
    {
#if NET7_0_OR_GREATER
        //there is no way Vector64 not hardware accelerated
        if(Vector64.IsHardwareAccelerated)
        {
            Vector64<ushort> bits = Vector64.Equals(Vector64.LoadUnsafe(ref _keysAndValues._id0), Vector64.Create(value));
            int index = BitOperations.TrailingZeroCount(bits.ExtractMostSignificantBits());
            Debugger.Break();
            return index;
        }
#endif

        if (value == _keysAndValues._id0)
            return 0;
        if (value == _keysAndValues._id1)
            return 1;
        if (value == _keysAndValues._id2)
            return 2;
        if (value == _keysAndValues._id3)
            return 3;
        return 32;
    }

    public ushort Lookup(int index)
    {
        Debug.Assert(index < 4);
        return Unsafe.Add(ref _keysAndValues._id4, index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Set(ushort key, ushort value)
    {
        Unsafe.Add(ref _keysAndValues._id4, _nextIndex) = value;
        _nextIndex = (_nextIndex + 1) & 3;
    }
}