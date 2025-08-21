using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
#if !NETSTANDARD
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace Frent.Collections;

internal struct Bitset
{
    internal static Bitset Zero = default;

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
    public Bitset CompSet(int index)
    {
        if (index != 0)
        {
            ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
            lane |= HighBit >> (index & 63);
        }

        return this;
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
            (a._0 & b._0) |
            (a._1 & b._1) |
            (a._2 & b._2) |
            (a._3 & b._3)) != 0;
#else
        // TODO: use native memory and Vector256.LoadAlignedNonTemporal?

        Vector256<ulong> vec1 = Vector256.LoadUnsafe(ref a._0);
        Vector256<ulong> vec2 = Vector256.LoadUnsafe(ref b._0);

        if (Avx.IsSupported)
        {
            return !Avx.TestZ(vec1, vec2);
        }

        return (vec1 & vec2) != Vector256<ulong>.Zero;
#endif
    }

#if !NETSTANDARD
    public Vector256<ulong> AsVector() => Vector256.LoadUnsafe(ref _0);
    /// <summary>
    /// True if pass, false if fail
    /// </summary>
    /// <remarks>
    /// Should be aligned
    /// </remarks>
    public static bool Filter(ref Bitset set, Vector256<ulong> include, Vector256<ulong> exclude)
    {
        Vector256<ulong> self = Vector256.LoadUnsafe(ref set._0);

        if(Avx.IsSupported)
        {
            return Avx.TestC(self, include) && Avx.TestZ(self, exclude);
        }
        // remaining bits == fail
        return (include & self) == include && (exclude & self) == Vector256<ulong>.Zero;
    }

    public static void AssertHasSparseComponents(ref Bitset sparseBits, ref Bitset include)
    {
        Vector256<ulong> self = Vector256.LoadUnsafe(ref sparseBits._0);
        Vector256<ulong> includeVec = Vector256.LoadUnsafe(ref include._0);

        if (Avx.TestC(self, includeVec))
            return;

        FrentExceptions.Throw_NullReferenceException();
    }
#else
    public static bool Filter(ref Bitset self, Bitset include, Bitset exclude)
    {
        return (include & self) == include && (exclude & self) == default;
    }

    public static void AssertHasSparseComponents(ref Bitset sparseBits, ref Bitset include)
    {
        if ((include & sparseBits) == include)
            return;

        Unsafe.NullRef<int>() = 0;
    }

    public static Bitset operator &(Bitset l, Bitset r) => new Bitset
    {
        _0 = l._0 & r._0,
        _1 = l._1 & r._1,
        _2 = l._2 & r._2,
        _3 = l._3 & r._3,
    };

    public static bool operator ==(Bitset l, Bitset r) =>
        l._0 == r._0 &&
        l._1 == r._1 &&
        l._2 == r._2 &&
        l._3 == r._3;

    public static bool operator !=(Bitset l, Bitset r) =>
        !(l == r);

    public override bool Equals(object obj) => obj is Bitset b && 
        b._0 == _0 &&
        b._1 == _1 &&
        b._2 == _2 &&
        b._3 == _3
        ;

    public override int GetHashCode() => (_0 ^ _1 ^ _2 ^ _3).GetHashCode();
#endif

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
