using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

//160 bits, 20 bytes
internal struct ArchetypeNeighborCache
{
    //128 bits
    private InlineArray8<ushort> _keysAndValues;
    //32
    private int _nextIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Traverse(ushort value)
    {
        //my simd code is garbage
        //#if NET7_0_OR_GREATER
        //        if(Vector256.IsHardwareAccelerated)
        //        {
        //            Vector256<ushort> bits = Vector256.Equals(Vector256.LoadUnsafe(ref _keysAndValues._0), Vector256.Create(value));
        //            int index = BitOperations.TrailingZeroCount(bits.ExtractMostSignificantBits());
        //            return index;
        //        }
        //#endif
        //TODO: better impl
        if (value == _keysAndValues._0)
            return 0;
        if (value == _keysAndValues._1)
            return 1;
        if (value == _keysAndValues._2)
            return 2;
        if (value == _keysAndValues._3)
            return 3;

        return 32;
    }

    public ushort Lookup(int index)
    {
        Debug.Assert(index < 4);
        return Unsafe.Add(ref _keysAndValues._4, index);
    }

    public void Set(ushort key, ushort value)
    {
        Unsafe.Add(ref _keysAndValues._4, _nextIndex) = value;
        Unsafe.Add(ref _keysAndValues._0, _nextIndex) = key;
        _nextIndex = (_nextIndex + 1) & 3;
    }
}