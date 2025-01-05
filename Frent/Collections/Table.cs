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
            if (index < buffer.Length)
                return ref buffer[index];
            return ref ResizeGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T ResizeGet(uint index)
    {
        Array.Resize(ref _buffer, (int)BitOperations.RoundUpToPowerOf2(index + 1));
        //this is faster for larger entity counts
        //but ensure capacity does essentially the same thing
        //Array.Resize(ref _buffer, (int)(index << 2));
        return ref _buffer[index];
    }

    public void EnsureCapacity(int size)
    {
        if (_buffer.Length >= size)
            return;
        Array.Resize(ref _buffer, size);
    }

    public Span<T> AsSpan() => _buffer.AsSpan();
}