using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Frent.Core;

namespace Frent.Collections;

internal sealed class SparseSet<T>
{
    private int _nextIndex;
    private T[] _dense;
    private int[] _sparse;

    public ref T this[int id]
    {
        get
        {
            ref var index = ref EnsureSparseCapacityAndGetIndex(id);

            if (index == -1)
                index = _nextIndex++;

            return ref MemoryHelpers.GetValueOrResize(ref _dense, index);
        }
    }

    public SparseSet()
    {
        const int InitialCapacity = 4;
        _dense = new T[InitialCapacity];
        _sparse = new int[InitialCapacity];
        _sparse.AsSpan().Fill(-1);
    }

    private ref int EnsureSparseCapacityAndGetIndex(int id)
    {
        var localSparse = _sparse;
        if (id < localSparse.Length)
            return ref localSparse[id];

        return ref ResizeArrayAndGet(ref _sparse, id);

        static ref int ResizeArrayAndGet(ref int[] arr, int index)
        {
            int prevLen = arr.Length;
            Array.Resize(ref arr, (int)BitOperations.RoundUpToPowerOf2((uint)index + 1));
            arr.AsSpan(prevLen).Fill(-1);
            return ref arr[index];
        }
    }
}

internal abstract class SparseSetBase
{
    public abstract object Get(int id);
    public abstract void Set(int id, object value);
    public abstract void Add(int id, object value);
    public abstract void Remove(int id);
    public abstract bool Has(int id);
    public abstract bool TryGet(int id, out object value);
}