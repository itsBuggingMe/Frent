using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

internal abstract partial class ComponentStorage<TComponent> : ComponentStorageBase
{
    internal override void Release(Archetype archetype, bool isDeferredCreate)
    {
        if(!isDeferredCreate && Component<TComponent>.Destroyer is { } destroyer)
        {
            foreach(ref var component in AsSpanLength(archetype.EntityCount))
            {
                destroyer.Invoke(ref component);
            }
        }
        //TODO: return to pool here
    }
    //TODO: pool
    internal override void ResizeBuffer(int size) => Resize(size);
    //Note - no unsafe here
    internal override void SetAt(object component, int index) => this[index] = (TComponent)component;
    internal override object GetAt(int index) => this[index]!;
    internal override void InvokeGenericActionWith(GenericEvent? action, Entity e, int index) => action?.Invoke(e, ref this[index]);
    internal override void InvokeGenericActionWith(IGenericAction action, int index) => action?.Invoke(ref this[index]);
    internal override void PullComponentFromAndClear(ComponentStorageBase otherRunner, int me, int other, int otherRemoveIndex)
    {
        ComponentStorage<TComponent> componentRunner = UnsafeExtensions.UnsafeCast<ComponentStorage<TComponent>>(otherRunner);

        // see comment in ComponentStorageBase.PullComponentFromAndClearTryDevirt
        ref var item = ref componentRunner[other];
        this[me] = item;

        ref var downItem = ref componentRunner[otherRemoveIndex];
        item = downItem;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            downItem = default;
        }
    }
    internal override void PullComponentFrom(IDTable storage, int me, int other)
    {
        ref var item = ref ((IDTable<TComponent>)storage).Buffer[other];
        this[me] = item;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            item = default;
    }

    internal override void Delete(DeleteComponentData data)
    {
        ref var from = ref this[data.FromIndex];
        Component<TComponent>.Destroyer?.Invoke(ref from);
        this[data.ToIndex] = from;


        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    internal override ComponentHandle Store(int componentIndex)
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
internal unsafe abstract partial class ComponentStorage<TComponent>(int length) : ComponentStorageBase(length == 0 ? [] : new TComponent[length])
{
    public ref TComponent this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref TypedBuffer.UnsafeArrayIndex(index);
        }
    }

    private ref TComponent[] TypedBuffer => ref Unsafe.As<Array, TComponent[]>(ref _buffer);

    protected void Resize(int size)
    {
        Array.Resize(ref TypedBuffer, size);
    }


#if NETSTANDARD2_1
    public Span<TComponent> AsSpanLength(int length) => TypedBuffer.AsSpan(0, length);
    public Span<TComponent> AsSpan() => TypedBuffer;
#else
    public Span<TComponent> AsSpanLength(int length) => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(TypedBuffer), length);
    public Span<TComponent> AsSpan() => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(TypedBuffer), TypedBuffer.Length);
#endif

    public ref TComponent GetComponentStorageDataReference() => ref MemoryMarshal.GetArrayDataReference(TypedBuffer);

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