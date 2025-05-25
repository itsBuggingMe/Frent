using Frent.Buffers;
using Frent.Core;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

internal struct FastStack<T>(int initalComponents) : IEnumerable<T>
{
    [DebuggerStepThrough]
    public static FastStack<T> Create(int initalComponents) => new FastStack<T>(initalComponents);
    public static FastStack<T> Create(T[] initalBuffer) => new FastStack<T>()
    {
        _buffer = initalBuffer
    };

    private T[] _buffer = new T[initalComponents];
    private int _nextIndex = 0;

    public readonly int Count => _nextIndex;
    public readonly T Top => _buffer[_nextIndex - 1];
    public readonly bool HasElements => _nextIndex > 0;

    public readonly ref T this[int i] => ref _buffer[i];


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T comp)
    {
        var buffer = _buffer;
        if ((uint)_nextIndex < (uint)buffer.Length)
            buffer[_nextIndex++] = comp;
        else
            ResizeAndPush(comp);
    }

    private void ResizeAndPush(in T comp)
    {
        FastStackArrayPool<T>.ResizeArrayFromPool(ref _buffer, _buffer.Length * 2);
        _buffer[_nextIndex++] = comp;
    }

    public void Compact() => Array.Resize(ref _buffer, _nextIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        var buffer = _buffer;
        var next = buffer[--_nextIndex];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            buffer[_nextIndex] = default!;
        return next;
    }

    [DebuggerStepThrough]
    public bool TryPop([NotNullWhen(true)] out T? value)
    {
        if (_nextIndex == 0)
        {
            value = default;
            return false;
        }

        //we can ignore - as the as the user doesn't push null onto the stack
        //they won't get null from the stack
        ref var slot = ref _buffer.UnsafeArrayIndex(--_nextIndex)!;
        value = slot;
        if(RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            slot = default;
        return true;
    }

    public void RemoveAtReplace(int index)
    {
        Debug.Assert(Count > 0);

        var buffer = _buffer;
        if (index < buffer.Length)
        {
            buffer[index] = buffer[--_nextIndex];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                buffer[_nextIndex] = default!;
        }
    }


    /// <summary>
    /// DO NOT ALTER WHILE SPAN IS IN USE
    /// </summary>
#if NETSTANDARD2_1
    public readonly Span<T> AsSpan() => _buffer.AsSpan(0, _nextIndex);
#else
    public readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buffer), _nextIndex);
#endif

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            AsSpan().Clear();
        _nextIndex = 0;
    }

    public void ClearWithoutClearingGCReferences() => _nextIndex = 0;

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public readonly FastStackEnumerator GetEnumerator() => new(this);

    public struct FastStackEnumerator(FastStack<T> stack) : IEnumerator<T>
    {
        private T[] _elements = stack._buffer;
        private int _max = stack._nextIndex;
        private int _index = -1;
        public readonly T Current => _elements[_index];
        readonly object? IEnumerator.Current => _elements[_index];
        public void Dispose() => _elements = null!;
        public bool MoveNext() => ++_index < _max;
        public void Reset() => _index = -1;
    }
}
