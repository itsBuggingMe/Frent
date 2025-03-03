using Frent.Core.Events;
using System.Runtime.CompilerServices;

namespace Frent.Collections;
internal abstract class IDTable
{
    protected Array _buffer;
    protected NativeStack<int> _recycled;
    protected int _nextIndex;
    private bool _hasGCReferences;

    public IDTable(Array empty, bool gcRefs)
    {
        _buffer = empty;
        _hasGCReferences = gcRefs;
        _recycled = new NativeStack<int>(2);
    }

    public int CreateBoxed(object toStore)
    {
        int index;
        if (_recycled.CanPop())
        {
            index = _recycled.PopUnsafe();
        }
        else
        {
            index = _nextIndex++;
            if (index == _buffer.Length)
                Double();
        }

        SetValue(toStore, index);

        return index;
    }

    public object GetValueBoxed(int index) => GetValue(index);

    public object TakeBoxed(int index)
    {
        _recycled.Push() = index;
        return GetValue(index);
    }

    public void Consume(int index)
    {
        _recycled.Push() = index;
        if (_hasGCReferences)
        {
            ClearValue(index);
        }
    }

    public abstract void InvokeEventWithAndConsume(GenericEvent? genericEvent, Entity entity, int index);
    protected abstract void SetValue(object value, int index);
    protected abstract void ClearValue(int index);
    protected abstract object GetValue(int index);
    protected abstract void Double();
}

internal class IDTable<T> : IDTable
{
    public IDTable() : base(Array.Empty<T>(), RuntimeHelpers.IsReferenceOrContainsReferences<T>()) { }
    public ref T[] Buffer => ref Unsafe.As<Array, T[]>(ref _buffer);

    public ref T Create(out int index)
    {
        if (_recycled.CanPop())
        {
            index = _recycled.PopUnsafe();
        }
        else
        {
            index = _nextIndex++;
            if (index == _buffer.Length)
                Double();
        }

        return ref Buffer[index];
    }

    public override void InvokeEventWithAndConsume(GenericEvent? genericEvent, Entity entity, int index)
    {
        genericEvent?.Invoke(entity, ref Buffer[index]);
        _recycled.Push() = index;
    }

    public ref T Take(int index)
    {
        return ref Buffer[index];
    }

    protected override void Double()
    {
        Array.Resize(ref Buffer, Math.Max(Buffer.Length << 1, 1));
    }

    protected override object GetValue(int index) => Buffer[index]!;
    protected override void SetValue(object value, int index) => Buffer[index] = (T)value;
    protected override void ClearValue(int index) => Buffer[index] = default!;
}