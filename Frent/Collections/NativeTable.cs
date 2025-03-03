using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

#if NETSTANDARD2_1
//Do not pass around this struct by value!!!
//You must use the constructor when initalizating!!!

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
//As long as the user always uses the ctor, it would throw when managed type is used
internal unsafe struct NativeTable<T> : IDisposable where T : struct
{
    private T[] _array;
    private int _length;

    public ref T this[int index]
    {
        get
        {
#if DEBUG
            if(index < 0)
                throw new ArgumentOutOfRangeException();
#endif
            if (index >= _length)
                return ref ResizeFor(index);
            return ref _array[index];
        }
    }

    public ref T UnsafeIndexNoResize(int index)
    {
        return ref _array[index];
    }

    public NativeTable(int initalCapacity)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException("Cannot store managed objects in native code");
        if(initalCapacity < 1)
            throw new ArgumentOutOfRangeException();

        _length = initalCapacity;
        _array = new T[initalCapacity];
    }

    public void Dispose()
    {

    }

    private ref T ResizeFor(int index)
    {
        _length = checked((int)BitOperations.RoundUpToPowerOf2((uint)(index + 1)));
        Array.Resize(ref _array, _length);
        return ref _array[index];
    }

    public void EnsureCapacity(int newCapacity)
    {
        _length = checked((int)BitOperations.RoundUpToPowerOf2((uint)newCapacity));
        Array.Resize(ref _array, _length);
    }

    public Span<T> AsSpan() => _array.AsSpan(0, _length);

    internal Span<T> Span => AsSpan();
}
#else
//Do not pass around this struct by value!!!
//You must use the constructor when initalizating!!!

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
//As long as the user always uses the ctor, it would throw when managed type is used
internal unsafe struct NativeTable<T> : IDisposable where T : struct
{
    private static readonly nuint Size = (nuint)Unsafe.SizeOf<T>();
    private T* _array;
    private int _length;

    public ref T this[int index]
    {
        get
        {
#if DEBUG
            if (index < 0)
                throw new ArgumentOutOfRangeException();
#endif
            if (index >= _length)
                return ref ResizeFor(index);
            return ref _array[index];
        }
    }

    public ref T UnsafeIndexNoResize(int index)
    {
        return ref _array[index];
    }

    public NativeTable(int initalCapacity)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException("Cannot store managed objects in native code");
        if (initalCapacity < 1)
            throw new ArgumentOutOfRangeException();

        _length = initalCapacity;
        _array = (T*)NativeMemory.Alloc((nuint)initalCapacity * Size);
    }

    public void Dispose()
    {
        NativeMemory.Free(_array);
        //null reference isnt as bad as a use after free, right?
        _array = (T*)0;
    }

    private ref T ResizeFor(int index)
    {
        _length = checked((int)BitOperations.RoundUpToPowerOf2((uint)(index + 1)));
        _array = (T*)NativeMemory.Realloc(_array, (nuint)_length * Size);
        return ref _array[index];
    }

    public void EnsureCapacity(int newCapacity)
    {
        _length = checked((int)BitOperations.RoundUpToPowerOf2((uint)newCapacity));
        _array = (T*)NativeMemory.Realloc(_array, (nuint)_length * Size);
    }

    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_array), _length);

    internal Span<T> Span => AsSpan();
}
#endif