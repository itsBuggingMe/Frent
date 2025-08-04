using System.IO.Pipes;
using System.Numerics;

namespace Frent.Collections;

internal struct Bitset(int capacity) : IEnumerable<int>
{
    private nint[] _bits = new nint[Divide(capacity)];
    public readonly int Length => (int)Multiply(_bits.Length);

    public void SetOrResize(int index)
    {
        var loc = _bits;

        if(!((uint)index < (uint)loc.Length))
        {
            ResizeAndSet(index);
            return;
        }

        loc[Divide(index)] |= (nint)1 << Mod(index);
    }

    private void ResizeAndSet(int index)
    {
        Array.Resize(ref _bits, (int)BitOperations.RoundUpToPowerOf2((uint)(Divide(index) + 1)));
        _bits[Divide(index)] |= (nint)1 << Mod(index);
    }

    public bool IsSet(int index)
    {
        var loc = _bits;

        if (!((uint)index < (uint)loc.Length))
        {
            return false;
        }

        return (loc[Divide(index)] & ((nint)1 << Mod(index))) != 0;
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

        // who is running this on a 16 bit machine?
        throw new NotSupportedException();
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

        throw new NotSupportedException();
    }
    public Enumerator GetEnumerator() => new(this);

    internal struct Enumerator(Bitset bitset)
    {
        private nint[] _bits = bitset._bits;
        private nint _consumedBits = 0;
        private int _index = 0;
        private int _current;
        public int Current => _current;
        public bool MoveNext()
        {
            if(_consumedBits == 0)
            {
                if ((uint)_index < (uint)_bits.Length)
                {
                    _current = _index * IntPtr.Size * 8;
                    _consumedBits = _bits[_index++];
                    return true;
                }

                return false;
            }

            int leadingZeros = BitOperations.LeadingZeroCount((nuint)_consumedBits);

            _consumedBits <<= leadingZeros + 1;
            _current += leadingZeros;

            return true;
        }
    }
}
