using System.Numerics;

namespace Frent.Collections;

internal struct Bitset(int capacity)
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

        loc[Divide(index)] |= 1 << Mod(index);
    }

    private void ResizeAndSet(int index)
    {
        Array.Resize(ref _bits, (int)BitOperations.RoundUpToPowerOf2((uint)(Divide(index) + 1)));
        _bits[Divide(index)] |= 1 << Mod(index);
    }

    public bool IsSet(int index)
    {
        var loc = _bits;

        if (!((uint)index < (uint)loc.Length))
        {
            return false;
        }

        return (loc[Divide(index)] & (1 << Mod(index))) != 0;
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
}
