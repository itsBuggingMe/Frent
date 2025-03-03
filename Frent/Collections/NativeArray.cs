using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

#if !NETSTANDARD2_1
//Do not pass around this struct by value!!!
//You must use the constructor when initalizating!!!

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
//As long as the user always uses the ctor, it would throw when managed type is used
internal unsafe struct NativeArray<T> : IDisposable
{
    public int Length => _length;

    private static readonly nuint Size = (nuint)Unsafe.SizeOf<T>();
    private T* _array;
    private int _length;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if(index >= _length || index < 0)
                throw new IndexOutOfRangeException();
#endif
            return ref _array[index];
        }
    }

    public NativeArray(int length)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException("Cannot store managed objects in native code");
        if(length < 1)
            throw new ArgumentOutOfRangeException();

        _length = length;
        _array = (T*)NativeMemory.Alloc((nuint)length * Size);
    }

    public void Resize(int size)
    {
        _length = size;
        _array = (T*)NativeMemory.Realloc(_array, Size * (nuint)size);
    }

    public void Dispose()
    {
        NativeMemory.Free(_array);
        //null reference isnt as bad as a use after free, right?
        _array = (T*)0;
    }

    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_array), _length);
    public Span<T> AsSpanLen(int len)
    {
        System.Diagnostics.Debug.Assert(len <= _length);
        return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_array), len);
    }
}
#endif