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
using Frent.Core.Events;

namespace Frent.Collections;

internal sealed class ComponentSparseSet<T> : ComponentSparseSetBase
{
    private int _nextIndex;
    private T[] _dense = new T[InitialCapacity];

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

    /// <remarks>This differs from <see cref="this[int]"/> as it throws if the item does not exist.</remarks>
    public ref T GetTyped(int id) => ref _dense[_sparse[id]]!;

    public override object Get(int id) => _dense[_sparse[id]]!;

    public override void Set(int id, object value) => _dense[_sparse[id]] = (T)value;

    public override void Add(int id, ComponentHandle value) => this[id] = value.Retrieve<T>();

    public override void Remove(int id)
    {
        ref var index = ref _sparse[id];
        ref var slot = ref _dense[index];
        Component<T>.Destroyer?.Invoke(ref slot);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            slot = default!;
        index = -1;
    }

    public override bool TryGet(int id, out object value)
    {
        throw new NotImplementedException();
    }

    public Ref<T> TryGet(out bool exists)
    {
        throw new NotImplementedException();
    }

    public override void Add(int id, object value)
    {
        throw new NotImplementedException();
    }

    public override void InvokeGenericEvent(int id, GenericEvent @event)
    {

    }
}

internal abstract class ComponentSparseSetBase
{
    protected int[] _sparse;
    protected const int InitialCapacity = 4;

    protected ComponentSparseSetBase()
    {
        _sparse = new int[InitialCapacity];
        _sparse.AsSpan().Fill(-1);
    }

    public abstract object Get(int id);
    public abstract void Set(int id, object value);
    public abstract void Add(int id, ComponentHandle value);
    public abstract void Add(int id, object value);
    public abstract void Remove(int id);
    public abstract bool TryGet(int id, out object value);
    public abstract void InvokeGenericEvent(int id, GenericEvent @event);

    public bool Has(int id)
    {
        int[] arr = _sparse;
        return (uint)id < (uint)arr.Length && arr[id] == -1;
    }

    protected ref int EnsureSparseCapacityAndGetIndex(int id)
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