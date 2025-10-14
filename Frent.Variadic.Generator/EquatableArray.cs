using System;
using System.Collections;
using System.Collections.Generic;

namespace Frent.Variadic.Generator;

internal struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static EquatableArray<T> Empty => new([]);

    public readonly T[] Items;
    public readonly int Length => Items.Length;

    public EquatableArray(T[] items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        Items = items;
    }
    public EquatableArray(int len) : this(new T[len]) { }


    public static bool operator ==(EquatableArray<T> a, EquatableArray<T> b)
        => a.Equals(b);
    public static bool operator !=(EquatableArray<T> a, EquatableArray<T> b)
        => !a.Equals(b);
    public override bool Equals(object obj)
        => obj is EquatableArray<T> n && n == this;
    public readonly bool Equals(EquatableArray<T> other)
    {
        if(other.Items is null)
            return Items is null;

        if (Items is null)
            return false;
        
        return Items.AsSpan().SequenceEqual(other.Items);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (ref var value in Items.AsSpan())
        {
            hashCode.Add(value);
        }
        return hashCode.ToHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EquatableArrayEnumerator(this);
    public EquatableArrayEnumerator GetEnumerator() => new EquatableArrayEnumerator(this);

    public struct EquatableArrayEnumerator : IEnumerator<T>
    {
        private readonly T[] _items;
        private int _index;

        public EquatableArrayEnumerator(EquatableArray<T> equatableArray)
        {
            _index = -1;
            _items = equatableArray.Items;
        }

        public T Current => _items[_index];
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _items.Length;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }

    public static implicit operator ReadOnlySpan<T>(EquatableArray<T> values) => values.Items;
}