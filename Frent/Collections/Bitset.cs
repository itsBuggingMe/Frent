using System.Numerics;
using System.Runtime.CompilerServices;
#if !NETSTANDARD
using System.Runtime.Intrinsics;
#endif

namespace Frent.Collections;

internal struct Bitset
{
    public const int Capacity = 256;
    private const ulong HighBit = 1UL << 63;
    private ulong _0;
    private ulong _1;
    private ulong _2;
    private ulong _3;

    public bool IsDefault => _0 == default &&
        _1 == default &&
        _2 == default &&
        _3 == default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index)
    {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane |= HighBit >> (index & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearAt(int index)
    {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane &= ~(HighBit >> (index & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(int index)
    {
        ulong lane = Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        return (lane & (HighBit >> (index & 63))) != 0;
    }

    /// <summary>
    /// Calculate the bitwise AND of two bitsets and returns true if any bit is remaining
    /// </summary>
    public static bool AndAndThenAnySet(ref Bitset a, ref Bitset b)
    {
#if NETSTANDARD
        return (
            (a._0 & a._0) |
            (a._1 & a._1) |
            (a._2 & a._2) |
            (a._3 & a._3)) != 0;
#else
        Vector256<ulong> vec1 = Unsafe.As<ulong, Vector256<ulong>>(ref a._0);
        Vector256<ulong> vec2 = Unsafe.As<ulong, Vector256<ulong>>(ref b._0);

        return (vec1 & vec2) != Vector256<ulong>.Zero;
#endif
    }

    internal int? TryFindIndexOfBitGreaterThanOrEqualTo(int bitIndex)
    {
        if (!((uint)bitIndex < Capacity))
            return null;

        int laneIndex = bitIndex >> 6;
        int bitOffset = (bitIndex) & 63;
        // 0101010000100000000000000000000000000000000000000000000000000001
        ulong bits = Unsafe.Add(ref _0, (nuint)laneIndex);
        bits &= ulong.MaxValue >> bitOffset;

        while (bits == 0)
        {
            laneIndex++;
            if (laneIndex >= 4)
                return null;
            bits = Unsafe.Add(ref _0, (nuint)laneIndex);
        }

        int leading = BitOperations.LeadingZeroCount(bits);
        return (laneIndex << 6) + leading;
    }

    public readonly Enumerator GetEnumerator() => new(this);

    public readonly int PopCnt() =>
        BitOperations.PopCount(_0) +
        BitOperations.PopCount(_1) +
        BitOperations.PopCount(_2) +
        BitOperations.PopCount(_3)
        ;

    internal ref struct Enumerator
    {
        private Bitset _set;
        private int _index;

        public Enumerator(Bitset bitset)
        {
            _set = bitset;
            _index = 0;
        }

        public int Current => _index - 1;

        public bool MoveNext()
        {
            int? index = _set.TryFindIndexOfBitGreaterThanOrEqualTo(_index);
            if(index is int x)
            {
                _index = x + 1;
                return true;
            }
            return false;
        }
    }
}
