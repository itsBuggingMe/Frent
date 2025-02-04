using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

//Do not pass around this struct by value!!!
//You must use the constructor when initalizating!!!

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
//As long as the user always uses the ctor, it would throw when managed type is used
internal unsafe struct NativeStack<T> : IDisposable where T : struct
{
    public int Count => _nextIndex;

    private static readonly nuint Size = (nuint)Unsafe.SizeOf<T>();
    private T* _array;
    private int _capacity;
    private int _nextIndex;

    public ref T this[int index]
    {
        get
        {
#if DEBUG
            if(index > _nextIndex || index < 0)
                throw new IndexOutOfRangeException();
#endif
            return ref _array[index];
        }
    }

    public NativeStack(int initalCapacity)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException("Cannot store managed objects in native code");
        if(initalCapacity < 1)
            throw new ArgumentOutOfRangeException();

        _capacity = initalCapacity;
        _array = (T*)NativeMemory.Alloc((nuint)initalCapacity * Size);
    }

    public ref T Push()
    {
        if (_nextIndex == _capacity)
            Resize();
        return ref _array[_nextIndex++];
    }

    public void Pop(out T value)
    {
        if (_nextIndex == 0)
            FrentExceptions.Throw_InvalidOperationException("Stack is empty!");
        value = _array[--_nextIndex];
    }

    public bool TryPop(out T value)
    {
        if(_nextIndex == 0)
        {
            Unsafe.SkipInit(out value);
            return false;
        }

        value = _array[--_nextIndex];
        return true;
    }

    public void RemoveAt(int index)
    {
        if((uint)index < (uint)_nextIndex)
        {
            _array[index] = _array[--_nextIndex];
            return;
        }
        FrentExceptions.Throw_InvalidOperationException("Invalid Index!");
    }

    private void Resize()
    {
        _capacity = checked(_capacity * 2);
        _array = (T*)NativeMemory.Realloc(_array, Size * (nuint)_capacity);
    }

    public void Dispose()
    {
        NativeMemory.Free(_array);
        //null reference isnt as bad as a use after free, right?
        _array = (T*)0;
    }

    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_array), _nextIndex);
}