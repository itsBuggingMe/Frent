using Frent.Core;
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
                return ref buffer.UnsafeArrayIndex(index);
            return ref ResizeGet(index);
        }
    }

    public ref T IndexWithInt(int index)
    {
        var buffer = _buffer;
        if (index < buffer.Length)
            return ref buffer.UnsafeArrayIndex(index);
        return ref ResizeGet((uint)index);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T ResizeGet(uint index)
    {
        MemoryHelpers<T>.ResizeArrayFromPool(ref _buffer, (int)BitOperations.RoundUpToPowerOf2(index + 1));
        return ref _buffer.UnsafeArrayIndex(index);
    }

    public ref T GetValueNoCheck(int index)
    {
        return ref _buffer.UnsafeArrayIndex(index);
    }

    public void EnsureCapacity(int size)
    {
        if (_buffer.Length >= size)
            return;
        MemoryHelpers<T>.ResizeArrayFromPool(ref _buffer, size);
    }

    public Span<T> AsSpan() => _buffer.AsSpan();
}