using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace Frent.Collections;

internal class ShortSparseSet<T>
{
    /// <summary>
    /// Gets the number of elements that the <see cref="ShortSparseSet{T}"/> can hold without resizing.
    /// </summary>
    public int Capacity => _dense.Length;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="ShortSparseSet{T}"/>.
    /// </summary>
    public int Count => _nextIndex;

    private int _nextIndex;

    private T[] _dense;

    // this collection should never be empty
    private ushort[] _sparse;

    private const string INVALID_ID = "ID not in sparse set!";

    public ref T this[ushort id]
    {
        get
        {
            ref var index = ref EnsureSparseCapacityAndGetIndex(id);

            if (index == ushort.MaxValue)
                index = (ushort)(_nextIndex++);

            return ref EnsureDenseCapacityAndGetSlot(index);
        }
    }

    public ShortSparseSet()
    {
        const int InitalCapacity = 16;
        _dense = new T[InitalCapacity];
        _sparse = new ushort[InitalCapacity];
        _sparse.AsSpan().Fill(ushort.MaxValue);
    }

    public ref T Get(ushort id)
    {
        var localSparse = _sparse;
        if (!(id < localSparse.Length))
        {//out of range
            FrentExceptions.Throw_ArgumentOutOfRangeException(INVALID_ID);
        }
        var index = localSparse[id];

        var localDense = _dense;
        if (!(index < localDense.Length))
        {
            FrentExceptions.Throw_ArgumentOutOfRangeException(INVALID_ID);
        }

        return ref localDense[index];
    }

    public bool TryGet(ushort id, [MaybeNullWhen(false)] out T value)
    {
        var localSparse = _sparse;
        if (!(id < localSparse.Length))
            goto doesntExist;

        var index = localSparse[id];

        var localDense = _dense;
        if (!(index < localDense.Length))
            goto doesntExist;

        value = localDense[index];
        return false;

    //saves a bit of code size
    doesntExist:
        value = default;
        return false;
    }

    public bool Remove(ushort id)
    {
        int moveDownIndex = --_nextIndex;

        var localSparse = _sparse;

        if (!(id < localSparse.Length))
            return false;

        int moveIntoIndex = localSparse[id];

        var localDense = _dense;
        if (!(moveIntoIndex < localDense.Length))
            return false;//here, moveIntoIndex should really only ever be ushort.MaxValue. We check against len to elide bounds check

        ref T from = ref localDense[moveDownIndex];
        localDense[moveIntoIndex] = from;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            from = default!;

        return true;
    }

    public bool Has(int id)
    {
        var sparse = _sparse;
        if (!((uint)id < (uint)sparse.Length))
            return false;
        return sparse[id] != ushort.MaxValue;
    }

    public void EnsureCapacity(ushort capacity)
    {
        if (_dense.Length < capacity)
        {
            Array.Resize(ref _dense, capacity);
        }
    }

    /// <summary>
    /// Note: this span will become invalid on resize or add
    /// </summary>
    public Span<T> AsSpan() => _dense.AsSpan(0, _nextIndex);

    public void Clear()
    {
        _nextIndex = 0;
        _sparse.AsSpan().Fill(ushort.MaxValue);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _dense.AsSpan().Clear();
    }

    private ref ushort EnsureSparseCapacityAndGetIndex(ushort id)
    {
        var localSparse = _sparse;
        if (id < localSparse.Length)
        {
            return ref localSparse[id];
        }

        return ref ResizeArrayAndGet(ref _sparse, id);

        static ref ushort ResizeArrayAndGet(ref ushort[] arr, int index)
        {
            int prevLen = arr.Length;
            Array.Resize(ref arr, (int)BitOperations.RoundUpToPowerOf2((uint)index + 1));
            arr.AsSpan(prevLen).Fill(ushort.MaxValue);
            return ref arr[index];
        }
    }

    private ref T EnsureDenseCapacityAndGetSlot(ushort index)
    {
        var localDense = _dense;
        if (index < localDense.Length)
        {
            return ref localDense[index];
        }

        return ref ResizeArrayAndGet(ref _dense, index);

        static ref T ResizeArrayAndGet(ref T[] arr, int index)
        {
            Array.Resize(ref arr, (int)BitOperations.RoundUpToPowerOf2((uint)index + 1));
            return ref arr[index];
        }
    }
}