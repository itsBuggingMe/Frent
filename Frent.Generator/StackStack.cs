using System;

namespace Frent.Generator;
internal ref struct StackStack<T>
{
    private Span<T> _buffer;
    private T[]? _array;
    private int _index;

    public StackStack(Span<T> buff)
    {
        _buffer = buff;
    }

    public void Push(T val)
    {
        if (_index >= _buffer.Length)
        {
            _array = new T[_buffer.Length * 2];
            _buffer.CopyTo(_array);
            _buffer = _array;
        }

        _buffer[_index++] = val;
    }

    public T[] ToArray() => _array?.Length == _index ? _array : _buffer.Slice(0, _index).ToArray();
}
