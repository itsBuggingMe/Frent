using System;

namespace Frent.Generator.Structures;

internal ref struct Stack<T>
{
    private T[] _array;
    private int _index;
    public int Count => _index;

    public Span<T> AsSpan() => _array.AsSpan(0, _index);

    public Stack(int len)
    {
        _array = len == 0 ? [] : new T[len];
    }

    public Stack() : this(0)
    {

    }

    public void Push(T val)
    {
        if (_index >= _array.Length)
        {
            var newArr = new T[Math.Max(_array.Length * 2, 1)];
            _array.CopyTo(newArr.AsSpan());
            _array = newArr;
        }

        _array[_index++] = val;
    }

    public T[] ToArray()
    {
        if (_index == 0)
            return [];
        return _array?.Length == _index ? _array : _array.AsSpan().Slice(0, _index).ToArray();
    }
}
