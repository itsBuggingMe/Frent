using System.Diagnostics;

namespace Frent.Collections;
internal ref struct ValueStack<T>
{
    public ValueStack(Span<T> initalValues)
    {
        Debug.Assert(initalValues.Length > 0);
        _buffer = initalValues;
    }

    private Span<T> _buffer;
    private T[]? _array;
    private int _nextIndex;
    public Span<T> AsSpan() => _buffer.Slice(0, _nextIndex);

    public void Push(T value)
    {
        if((uint)_nextIndex < (uint)_buffer.Length)
        {
            _buffer[_nextIndex++] = value;
        }

        ResizeAndPush(value);
    }

    private void ResizeAndPush(T value)
    {
        _array = new T[_buffer.Length * 2];
        _buffer.CopyTo(_array);
        _buffer = _array;
        _buffer[_nextIndex++] = value;
    }
}
