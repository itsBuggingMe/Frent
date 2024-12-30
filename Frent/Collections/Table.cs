using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

internal struct Table<T>(int size)
{
    public static Table<T> Empty => new()
    {
        _buffer = []
    };

    private T[] _buffer = new T[size];

    public ref T this[uint index]
    {
        get
        {
            var buffer = _buffer;
            if(index < buffer.Length)
                return ref buffer[index];
            return ref ResizeGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T ResizeGet(uint index)
    {
        uint nextSize = BitOperations.RoundUpToPowerOf2(index + 1);
        Array.Resize(ref _buffer, (int)nextSize);
        return ref _buffer[index];
    }

    public Span<T> AsSpan() => _buffer.AsSpan();
}