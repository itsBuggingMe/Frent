using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

internal struct Bitset
{
    public const int Capacity = 256;

    private ulong _0;
    private ulong _1;
    private ulong _2;
    private ulong _3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index)
    {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane |= 1UL << (index & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearAt(int index)
    {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane &= ~(1UL << (index & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(int index)
    {
        ulong lane = Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        return (lane & (1UL << (index & 63))) != 0;
    }

    internal int? TryFindIndexOfBitGreaterThan(int bitIndex)
    {
        if ((uint)bitIndex >= Capacity)
            return null;

        int laneIndex = bitIndex >> 6;
        int bitOffset = (bitIndex + 1) & 63;

        ulong bits = Unsafe.Add(ref _0, (nuint)laneIndex);
        bits &= ulong.MaxValue << bitOffset;

        while (bits == 0)
        {
            laneIndex++;
            if (laneIndex >= 4)
                return null;
            bits = Unsafe.Add(ref _0, (nuint)laneIndex);
        }

        int trailing = BitOperations.LeadingZeroCount(bits);
        return (laneIndex << 6) + trailing;
    }

    public Enumerator GetEnumerator() => new(this);

    internal struct Enumerator
    {
        private ulong _laneBits;
        private int _lane;
        private int _current;
        private ulong _0, _1, _2, _3;

        public Enumerator(Bitset bitset)
        {
            _0 = bitset._0;
            _1 = bitset._1;
            _2 = bitset._2;
            _3 = bitset._3;
            _laneBits = 0;
            _lane = 0;
            _current = 0;
        }

        public int Current => _current;

        public bool MoveNext()
        {
            while (_laneBits == 0)
            {
                if (_lane >= 4)
                    return false;

                _current = _lane * 64;
                _laneBits = _lane switch
                {
                    0 => _0,
                    1 => _1,
                    2 => _2,
                    3 => _3,
                    _ => 0
                };
                _lane++;
            }

            int tz = BitOperations.LeadingZeroCount(_laneBits);
            _laneBits &= ~(1UL << tz);
            _current += tz;
            return true;
        }
    }
}
