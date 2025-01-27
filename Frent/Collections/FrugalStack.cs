using Frent.Buffers;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

internal struct FrugalStack<T>()
{
    private T[] _buffer = [];
    private int _nextIndex = 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T comp)
    {
        var buffer = _buffer;
        if ((uint)_nextIndex < (uint)buffer.Length)
            buffer[_nextIndex++] = comp;
        else
            ResizeAndPush(comp);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ResizeAndPush(in T comp)
    {
        FastStackArrayPool<T>.ResizeArrayFromPool(ref _buffer, _buffer.Length > 16 ? _buffer.Length << 1 : _buffer.Length + 2);
        _buffer[_nextIndex++] = comp;
    }

    public bool TryPop(out T value)
    {
        if (_nextIndex == 0)
        {
            value = default!;
            return false;
        }

        value = Pop();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        var next = _buffer[--_nextIndex];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _buffer[_nextIndex] = default!;
        return next;
    }

    public void Remove(T item)
    {
        int nextIndex = _nextIndex;
        Span<T> items = _buffer.AsSpan()[..nextIndex];
        for (int i = 0; i < nextIndex; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i], item))
            {
                items[i] = Pop();
                break;
            }
        }
    }


    /// <summary>
    /// DO NOT ALTER WHILE SPAN IS IN USE
    /// </summary>
    public readonly Span<T> AsSpan() => _buffer.AsSpan(0, _nextIndex);
}
