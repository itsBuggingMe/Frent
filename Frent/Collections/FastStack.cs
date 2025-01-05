using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent.Collections;
public struct FastStack<T>(int initalComponents) : IEnumerable<T>
{
    [DebuggerStepThrough]
    public static FastStack<T> Create(int initalComponents) => new FastStack<T>(initalComponents);

    private T[] _buffer = new T[initalComponents];
    private int _nextIndex = 0;

    private static bool NeedToWorryAboutGC => RuntimeHelpers.IsReferenceOrContainsReferences<T>();

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ResizeAndPush(in T comp)
    {
        Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_nextIndex++] = comp;
    }

    public void Compact() => Array.Resize(ref _buffer, _nextIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        var buffer = _buffer;
        var next = buffer[--_nextIndex];
        if (NeedToWorryAboutGC)
            buffer[_nextIndex] = default!;
        return next;
    }

    [DebuggerStepThrough]
    public bool TryPop([NotNullWhen(true)] out T? value)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new NotImplementedException();

        if (_nextIndex == 0)
        {
            value = default;
            return false;
        }

        //we can ignore - as the as the user doesn't push null onto the stack
        //they won't get null from the stack
        value = _buffer[--_nextIndex]!;
        return true;
    }

    public void RemoveAtReplace(int index)
    {
        Debug.Assert(Count > 0);

        var buffer = _buffer;
        if (index < buffer.Length)
        {
            buffer[index] = buffer[--_nextIndex];
            if (NeedToWorryAboutGC)
                buffer[_nextIndex] = default!;
        }
    }


    /// <summary>
    /// DO NOT ALTER WHILE SPAN IS IN USE
    /// </summary>
    public readonly Span<T> AsSpan() => new(_buffer, 0, _nextIndex);

    public void Clear()
    {
        if (NeedToWorryAboutGC)
            AsSpan().Clear();
        _nextIndex = 0;
    }


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
