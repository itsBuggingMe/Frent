using Frent.Buffers;
using Frent.Core;
using System;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Frent.Collections;

internal abstract class TrimmableStack
{
    protected Array _buffer;
    public Array Buffer => _buffer;

    public abstract int Push(object value);

    protected TrimmableStack(Array array)
    {
        _buffer = array;
    }
}

internal sealed class TrimmableStack<T> : TrimmableStack
{
    public T[] StrongBuffer => UnsafeExtensions.UnsafeCast<T[]>(Buffer);
    private int _nextIndex;

    public TrimmableStack() : base(new T[1])
    {
        Gen2GcCallback.Gen2CollectionOccured += Trim;
        World.ClearTempComponentStorage += Clear;
    }

    public void PushStronglyTyped(in T comp, out int index)
    {
        int len = StrongBuffer.Length;
        index = _nextIndex;
        if (!(_nextIndex < len))
            FastStackArrayPool<T>.ResizeArrayFromPool(ref Unsafe.As<Array, T[]>(ref _buffer), len << 1);
        StrongBuffer.UnsafeArrayIndex(_nextIndex++) = comp;
    }

    public override int Push(object value)
    {
        PushStronglyTyped((T)value, out int index);
        return index;
    }

    public void Clear()
    {
        if(RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            StrongBuffer.AsSpan().Clear();
        _nextIndex = 0;
    }

    public void Trim()
    {
        uint newSize = MemoryHelpers.RoundDownToPowerOfTwo((uint)_nextIndex);
        if (newSize == _buffer.Length)
            return;

        FastStackArrayPool<T>.ResizeArrayFromPool(ref Unsafe.As<Array, T[]>(ref _buffer), (int)newSize);
    }
}