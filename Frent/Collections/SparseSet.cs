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
    private T[] _dense = new T[InitialCapacity];

    public ref T this[int id]
    {
        get
        {
            ref var denseIndex = ref EnsureSparseCapacityAndGetIndex(id);

            if (denseIndex == -1)
            {
                denseIndex = _nextIndex++;
                _ids.UnsafeArrayIndex(denseIndex) = id;
            }

            return ref MemoryHelpers.GetValueOrResize(ref _dense, denseIndex);
        }
    }

    public override void AddOrSet(int id, ComponentHandle value) => this[id] = value.Retrieve<T>();

    public override void AddOrSet(int id, object value) => this[id] = (T)value;

    public override object Get(int id) => _dense[_sparse[id]]!;

    public override void InvokeGenericEvent(int id, Entity entity, GenericEvent @event) => @event.Invoke(entity, ref _dense[_sparse[id]]);

    public override void Remove(int id)
    {
        var sparse = _sparse;
        var dense = _dense;
        if (!((uint)id < (uint)sparse.Length))
            return;
        ref int denseIndexRef = ref sparse[id];
        int denseIndex = denseIndexRef;
        if (!((uint)denseIndex < (uint)dense.Length))
            return;

        ref var toRemove = ref dense[denseIndex];
        ref var top = ref dense[--_nextIndex];
        denseIndexRef = -1;

        toRemove = top;
        if(RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            top = default;
    }

    public override bool TryGet(int id, out object value)
    {
        var res = TryGet(id, out bool exists);
        value = res.Value!;
        return exists;
    }

    public Ref<T> TryGet(int id, out bool value)
    {
        var sparse = _sparse;
        var dense = _dense;
        if ((uint)id < (uint)sparse.Length)
        {
            int denseIndex = sparse[id];
            if ((uint)denseIndex < (uint)_nextIndex)
            {
                value = true;
                return new Ref<T>(dense, denseIndex);
            }
        }
        value = false;
        return default;
    }

    internal ref T GetComponentDataReference() => ref MemoryMarshal.GetArrayDataReference(_dense);
}

internal abstract class ComponentSparseSetBase
{
    protected int[] _sparse;
    protected int[] _ids;
    protected int _nextIndex;

    public int Count => _nextIndex;

    protected const int InitialCapacity = 4;

    protected ComponentSparseSetBase()
    {
        _sparse = new int[InitialCapacity];
        _ids = new int[InitialCapacity];
        _sparse.AsSpan().Fill(-1);
        _ids.AsSpan().Fill(-1);
    }

    public abstract object Get(int id);
    public abstract void AddOrSet(int id, ComponentHandle value);
    public abstract void AddOrSet(int id, object value);
    public abstract void Remove(int id);
    public abstract bool TryGet(int id, out object value);
    public abstract void InvokeGenericEvent(int id, Entity entity, GenericEvent @event);

    public bool Has(int id)
    {
        int[] arr = _sparse;
        return (uint)id < (uint)arr.Length && arr[id] != -1;
    }

    protected ref int EnsureSparseCapacityAndGetIndex(int id)
    {
        var localSparse = _sparse;
        if ((uint)id < (uint)localSparse.Length)
            return ref localSparse[id];

        ref int sparseIndex = ref ResizeArrayAndGet(ref _sparse, ref _ids, id);

        return ref sparseIndex;

        static ref int ResizeArrayAndGet(ref int[] sparse, ref int[] ids, int index)
        {
            int prevLen = sparse.Length;
            int newLen = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
            Array.Resize(ref sparse, newLen);
            Array.Resize(ref ids, newLen);
            sparse.AsSpan(prevLen).Fill(-1);
            return ref sparse[index];
        }
    }

    internal ref int GetEntityIDsDataReference() => ref MemoryMarshal.GetArrayDataReference(_ids);
    internal Span<int> SparseSpan() =>
#if NETSTANDARD
        _sparse.AsSpan()
#else
        MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_sparse), _sparse.Length)
#endif
        ;
}