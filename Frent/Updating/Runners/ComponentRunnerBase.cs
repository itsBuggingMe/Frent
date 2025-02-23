using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Core.Events;
using System.Diagnostics;
using System.Net.Security;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

internal abstract class ComponentRunnerBase<TSelf, TComponent> : ComponentStorage<TComponent>, IComponentRunner<TComponent>
    where TSelf : IComponentRunner<TComponent>, new()
{
    public abstract void Run(World world, Archetype b);
    public abstract void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    //TODO: improve
    public void Trim(int index) => Resize((int)BitOperations.RoundUpToPowerOf2((uint)index));
    //TODO: pool
    public void ResizeBuffer(int size) => Resize(size);
    //Note - no unsafe here
    public void SetAt(object component, int index) => this[index] = (TComponent)component;
    public object GetAt(int index) => this[index]!;
    public void InvokeGenericActionWith(GenericEvent? action, Entity e, int index) => action?.Invoke(e, ref this[index]);
    public void InvokeGenericActionWith(IGenericAction action, int index) => action?.Invoke(ref this[index]);
    public ComponentID ComponentID => Component<TComponent>.ID;

    public void PullComponentFromAndClear(IComponentRunner otherRunner, int me, int other, int otherRemoveIndex)
    {
        ComponentStorage<TComponent> componentRunner = UnsafeExtensions.UnsafeCast<ComponentStorage<TComponent>>(otherRunner);

        ref var item = ref componentRunner[other];
        this[me] = item;
        
        ref var downItem = ref componentRunner[otherRemoveIndex];
        item = downItem;

        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            downItem = default;
        }
    }

    public void PullComponentFrom(IDTable storage, int me, int other)
    {
        ref var item = ref ((IDTable<TComponent>)storage).Buffer[other];
        this[me] = item;

        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            item = default;
    }

    public void Delete(DeleteComponentData data)
    {
        ref var from = ref this[data.FromIndex];
        Component<TComponent>.Destroyer?.Invoke(ref from);
        this[data.ToIndex] = from;


        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    public ComponentHandle Store(int componentIndex)
    {
        ref var item = ref this[componentIndex];
        
        //we can't just copy to stack and run the destroyer on it
        //it is stored
        Component<TComponent>.Destroyer?.Invoke(ref item);

        Component<TComponent>.GeneralComponentStorage.Create(out var stackIndex) = item;
        return new ComponentHandle(stackIndex, Component<TComponent>.ID);
    }
}

#if MANAGED_COMPONENTS || TRUE
internal unsafe abstract class ComponentStorage<TComponent> : IDisposable
{
    public ref TComponent this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref _managed.UnsafeArrayIndex(index);
        }
    }

    public ComponentStorage()
    {
        _managed = new TComponent[1];
    }

    private TComponent[] _managed;

    protected void Resize(int size)
    {
        Array.Resize(ref _managed, size);
    }

    public Span<TComponent> AsSpan() => _managed;

    public Span<TComponent> AsSpan(int length) => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_managed), length);

    public ref TComponent GetComponentStorageDataReference() => ref MemoryMarshal.GetArrayDataReference(_managed);

    public void Dispose()
    {
        
    }
}
#else
internal unsafe abstract class ComponentStorage<TComponent> : IDisposable
{

    public ref TComponent this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            {
                return ref _managed!.UnsafeArrayIndex(index);
            }

            return ref _nativeArray[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentStorage()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            _managed = new TComponent[1];
        }
        else
        {
            _nativeArray = new(1);
        }
    }

    private TComponent[]? _managed;
    private NativeArray<TComponent> _nativeArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Resize(int size)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            Array.Resize(ref _managed, size);
        }
        else
        {
            _nativeArray.Resize(size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<TComponent> AsSpan() => RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>() ?
        _managed.AsSpan() : _nativeArray.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<TComponent> AsSpan(int length) => RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>() ?
        _managed.AsSpan(0, length) : _nativeArray.AsSpanLen(length);

    public void Dispose()
    {
        _nativeArray.Dispose();
    }
}
#endif