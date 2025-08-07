using System.IO.Pipes;
using System.Numerics;

namespace Frent.Collections;

internal struct Bitset
{
    public Bitset(int capacity) => _bits = new nuint[Divide(capacity) + 1];

    public Bitset() : this(0)
    {

    }

    private nuint[] _bits;
    public readonly int Length => Multiply(_bits.Length);
    private nuint HighBit => ((nuint)1 << (IntPtr.Size * 8 - 1));
    public void SetOrResize(int index)
    {
        var loc = _bits;

        if(!((uint)index < (uint)loc.Length))
        {
            ResizeAndSet(index);
            return;
        }

        loc[Divide(index)] |= HighBit >> Mod(index);
    }

    public void ClearAt(int index)
    {
        var loc = _bits;

        if (!((uint)index < (uint)loc.Length))
        {
            return;
        }

        loc[Divide(index)] ^= HighBit >> Mod(index);
    }

    private void ResizeAndSet(int index)
    {
        Array.Resize(ref _bits, (int)BitOperations.RoundUpToPowerOf2((uint)(Divide(index) + 1)));
        _bits[Divide(index)] |= HighBit >> Mod(index);
    }

    public bool IsSet(int index)
    {
        var loc = _bits;

        int chunkIndex = Divide(index);
        if (!((uint)chunkIndex < (uint)loc.Length))
        {
            return false;
        }

        return (loc[chunkIndex] & (HighBit >> Mod(index))) != 0;
    }

    private static int Mod(int value)
    {
        return value & (IntPtr.Size * 8 - 1);
    }

    private static int Divide(int value)
    {
        if(IntPtr.Size == 8)
        {
            return value >> 6;
        }
        else
        {
            return value >> 5;
        }
    }

    private static int Multiply(int value)
    {
        if (IntPtr.Size == 8)
        {
            return value << 6;
        }
        else
        {
            return value << 5;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    internal struct Enumerator(Bitset bitset)
    {
        private nuint[] _bits = bitset._bits;
        private nuint _consumedBits = 0;
        private int _index = 0;
        private int _current;
        public int Current => _current - 1;
        public bool MoveNext()
        {
            while(_consumedBits == 0)
            {
                if ((uint)_index < (uint)_bits.Length)
                {
                    _current = _index * IntPtr.Size * 8;
                    _consumedBits = _bits[_index++];
                }
                else
                {
                    return false;
                }
            }

            int leadingZeros = BitOperations.LeadingZeroCount(_consumedBits);

            int bitsToConsume = leadingZeros + 1;
            _consumedBits <<= bitsToConsume;
            _current += bitsToConsume;

            return true;
        }
    }
}
