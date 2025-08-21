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
    internal T[] Dense = new T[InitialCapacity];

    public ref T this[int id]
    {
        get
        {
            ref var denseIndex = ref EnsureSparseCapacityAndGetIndex(id);

            if (denseIndex == -1)
            {// creating new
                denseIndex = _nextIndex++;
                MemoryHelpers.GetValueOrResize(ref _ids, denseIndex) = id;
            }

            return ref MemoryHelpers.GetValueOrResize(ref Dense, denseIndex);
        }
    }

    public ref T AddComponent(int id)
    {
        ref var denseIndex = ref EnsureSparseCapacityAndGetIndex(id);
        if (denseIndex != -1)
        {
            FrentExceptions.Throw_ComponentAlreadyExistsException($"Component Already Has Component of Type {typeof(T).Name}!");
        }

        denseIndex = _nextIndex++;

        MemoryHelpers.GetValueOrResize(ref _ids, denseIndex) = id;
        return ref MemoryHelpers.GetValueOrResize(ref Dense, denseIndex);
    }

    public override void AddOrSet(int id, ComponentHandle value) => this[id] = value.Retrieve<T>();

    // match behavior with archetypical set
    public override void Set(Entity e, object value)
    {
        var dense = Dense;
        var sparse = _sparse;

        if(!((uint)e.EntityID < (uint)sparse.Length))
            FrentExceptions.Throw_ComponentNotFoundException($"Component of type {typeof(T).Name} does not exist on this entity.");
        int index = sparse[e.EntityID];
        if(!((uint)index < (uint)dense.Length))
            FrentExceptions.Throw_ComponentNotFoundException($"Component of type {typeof(T).Name} does not exist on this entity.");

        ref T toSet = ref dense[index];
        if (typeof(T).IsValueType)
        {// treat like writing a byref
            toSet = (T)value;
        }
        else
        {
            if (ReferenceEquals(toSet, value))
                return;
            toSet = (T)value;
            Component<T>.Initer?.Invoke(e, ref toSet);
        }
    }

#if NETSTANDARD
    public Ref<T> GetUnsafe(int id) => new Ref<T>(Dense, _sparse.UnsafeArrayIndex(id));
#else
    public ref T GetUnsafe(int id) => ref Dense.UnsafeArrayIndex(_sparse.UnsafeArrayIndex(id));
#endif

    public override object Get(int id) => Dense[_sparse[id]]!;

    public override void InvokeGenericEvent(Entity entity, GenericEvent @event) => @event.Invoke(entity, ref Dense[_sparse[entity.EntityID]]);

    public override void Remove(int id, bool call)
    {
        var sparse = _sparse;
        var dense = Dense;
        var ids = _ids;

        if (!((uint)id < (uint)sparse.Length))
            return;
        ref int denseIndexRef = ref sparse[id];
        int denseIndex = denseIndexRef;

        if (!((uint)denseIndex < (uint)dense.Length))
            return;

        ref var toRemove = ref dense[denseIndex];
        ref int toRemoveId = ref ids.UnsafeArrayIndex(denseIndex);

        if (call) Component<T>.Destroyer?.Invoke(ref toRemove);

        ref var top = ref dense.UnsafeArrayIndex(--_nextIndex);
        ref var topId = ref ids.UnsafeArrayIndex(_nextIndex);

        sparse[topId] = denseIndex;
        denseIndexRef = -1;

        toRemove = top;
        toRemoveId = topId;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) 
            top = default;
    }

    public override void Run(World world, ReadOnlySpan<int> ids) => Component<T>.BufferManagerInstance.RunSparse(this, world, ids);

    public override bool TryGet(int id, out object value)
    {
        var res = TryGet(id, out bool exists);
        value = res.Value!;
        return exists;
    }

    public Ref<T> TryGet(int id, out bool value)
    {
        var sparse = _sparse;
        var dense = Dense;
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

    internal ref T GetComponentDataReference() => ref MemoryMarshal.GetArrayDataReference(Dense);

    public override void Init(Entity entity) => Component<T>.Initer?.Invoke(entity, ref this[entity.EntityID]);
}

internal abstract class ComponentSparseSetBase
{
    protected int[] _sparse = [];
    protected int[] _ids = new int[InitialCapacity];
    protected int _nextIndex;

    public int Count => _nextIndex;

    protected const int InitialCapacity = 4;

    public abstract object Get(int id);
    public abstract void AddOrSet(int id, ComponentHandle value);
    public abstract void Set(Entity e, object value);
    public abstract void Remove(int id, bool callDestroyer);
    public abstract bool TryGet(int id, out object value);

    public abstract void Run(World world, ReadOnlySpan<int> ids);

    public abstract void Init(Entity id);
    public abstract void InvokeGenericEvent(Entity entity, GenericEvent @event);

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

        ref int sparseIndex = ref ResizeArrayAndGet(ref _sparse, id);

        return ref sparseIndex;

        static ref int ResizeArrayAndGet(ref int[] sparse, int index)
        {
            int prevLen = sparse.Length;
            int newLen = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
            Array.Resize(ref sparse, newLen);
            sparse.AsSpan(prevLen).Fill(-1);
            return ref sparse[index];
        }
    }

    internal ref int GetEntityIDsDataReference() => ref MemoryMarshal.GetArrayDataReference(_ids);
    internal Span<int> SparseSpan() =>
#if NETSTANDARD
        _sparse.AsSpan(0, _nextIndex)
#else
        MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_sparse), _nextIndex)
#endif
        ;

    internal Span<int> IDSpan() => _ids;
}