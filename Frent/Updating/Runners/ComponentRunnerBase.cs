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

    public void PullComponentFromAndClear(IComponentRunner otherRunner, int me, int other)
    {
        ComponentStorage<TComponent> componentRunner = UnsafeExtensions.UnsafeCast<ComponentStorage<TComponent>>(otherRunner);
        ref var item = ref componentRunner[other];
        this[me] = item;
        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            item = default;
        }
    }

    public void PullComponentFrom(TrimmableStack storage, int me, int other)
    {
        ref var item = ref ((TrimmableStack<TComponent>)storage).StrongBuffer[other];
        this[me] = item;

        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            item = default;
    }

    public void Delete(DeleteComponentData data)
    {
        ref var from = ref this[data.FromIndex];
        this[data.ToIndex] = from;

        if (Component<TComponent>.IsDestroyable)
        {
            ((IDestroyable)from!)?.Destroy();
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    public TrimmableStack PushComponentToStack(int componentIndex, out int stackIndex)
    {
        Component<TComponent>.TrimmableStack.PushStronglyTyped(this[componentIndex], out stackIndex);
        return Component<TComponent>.TrimmableStack;
    }
}

#if MANAGED_COMPONENTS
internal unsafe abstract class ComponentStorage<TComponent> : IDisposable
{

    public ref TComponent this[int index]
    {
        //this was not being inlined!!!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref _managed!.UnsafeArrayIndex(index);
        }
    }

    public ComponentStorage()
    {
        _managed = new TComponent[1];
    }

    private TComponent[]? _managed;

    protected void Resize(int size)
    {
        Array.Resize(ref _managed, size);
    }

    public Span<TComponent> AsSpan() => _managed;

    public Span<TComponent> AsSpan(int length) => _managed;

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

    public Span<TComponent> AsSpan() => RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>() ?
        _managed.AsSpan() : _nativeArray.AsSpan();

    public Span<TComponent> AsSpan(int length) => RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>() ?
        _managed.AsSpan(0, length) : _nativeArray.AsSpanLen(length);

    public void Dispose()
    {
        _nativeArray.Dispose();
    }
}
#endif