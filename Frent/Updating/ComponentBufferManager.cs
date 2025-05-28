using Frent.Collections;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Core.Events;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace Frent.Updating;

/// <summary>
/// Defines an object for creating component runners
/// </summary>
/// <remarks>This is really a glorified method table. Maybe replace with void**???</remarks>
/// <remarks>Used only in source generation</remarks>
internal abstract class ComponentBufferManager
{
    #region Things That Dont Need Buffer

    /// <summary>
    /// Used only in source generation
    /// </summary>
    internal abstract Array Create(int capacity);
    /// <summary>
    /// Used only in source generation
    /// </summary>
    internal abstract IDTable CreateTable();

    #endregion


    #region Things That Need More Type Info
    /// <summary>
    /// Calls the Update function on every component.
    /// </summary>
    internal abstract void Run(Array buffer, World world, Archetype b);
    /// <summary>
    /// Calls the Update function on the subsection of components.
    /// </summary>
    internal abstract void Run(Array buffer, World world, Archetype b, int start, int length);
    #endregion

    #region Things That Need Buffer & <T>
    /// <summary>
    /// Deletes a component from the storage.
    /// </summary>
    internal abstract void Delete(Array buffer, DeleteComponentData deleteComponentData);
    /// <summary>
    /// Resizes internal buffer to 0 and calls the destroyer on every component.
    /// </summary>
    internal abstract void Release(Array buffer, Archetype archetype, bool isDeferredCreate);
    /// <summary>
    /// Resizes internal buffer to the specified size. Does not call the destroyer.
    /// </summary>
    internal abstract void ResizeBuffer(ref Array buffer, int size);
    /// <summary>
    /// Only copies component. Initer and Destroyer not called.
    /// </summary>
    internal abstract void PullComponentFromAndClear(Array buffer, Array otherRunner, int me, int other, int otherRemove);
    /// <summary>
    /// Copies component from storage without disposing component handle - just copies. Initer called.
    /// </summary>
    internal abstract void PullComponentFrom(Array buffer, IDTable storage, int me, int other);
    /// <summary>
    /// Invokes generic event if not null.
    /// </summary>
    internal abstract void InvokeGenericActionWith(Array buffer, GenericEvent? action, Entity entity, int index);
    /// <summary>
    /// Invokes the generic action if not null.
    /// </summary>
    internal abstract void InvokeGenericActionWith(Array buffer, IGenericAction action, int index);
    /// <summary>
    /// Note: this method is pretty specialized. It creates a component handle and calls the destroyer.
    /// </summary>
    internal abstract ComponentHandle Store(Array buffer, int index);
    /// <summary>
    /// Sets the component at the index. Invokes lifetime if component type isn't a struct, the new component is different, and parent is not null.
    /// </summary>
    internal abstract void SetAt(Array buffer, Entity? parent, object component, int index);
    /// <summary>
    /// Sets the component at the index. Invokes lifetime if component type isn't a struct, the new component is different, and parent is not null.
    /// </summary>
    internal abstract void SetAt(Array buffer, Entity? parent, ComponentHandle component, int index);
    /// <summary>
    /// Calls the initer at the location.
    /// </summary>
    internal abstract void CallIniter(Array buffer, Entity parent, int index);
    #endregion
}

internal abstract class ComponentBufferManager<TComponent> : ComponentBufferManager
{
    internal sealed override Array Create(int capacity) => new TComponent[capacity];

    internal sealed override IDTable CreateTable() => new IDTable<TComponent>();

    internal sealed override void Release(Array buffer, Archetype archetype, bool isDeferredCreate)
    {

        if (!isDeferredCreate && Component<TComponent>.Destroyer is { } destroyer)
        {
            TComponent[] casted = UnsafeExtensions.UnsafeCast<TComponent[]>(buffer);
            foreach (ref var component in casted.AsSpan(0, archetype.EntityCount))
            {
                destroyer.Invoke(ref component);
            }
        }
        //TODO: return to pool here
    }
    //TODO: pool
    internal sealed override void ResizeBuffer(ref Array buffer, int size)
    {
#if DEBUG
        TComponent[] arr = (TComponent[])buffer;
        Array.Resize(ref arr, size);
        buffer = arr;
#else
        Array.Resize(ref Unsafe.As<Array, TComponent[]>(ref buffer), size);
#endif
    }
    //Note - no unsafe here
    internal sealed override void SetAt(Array buffer, Entity? parent, object component, int index)
    {
        ref TComponent slot = ref Index(buffer, index);
        if (!typeof(TComponent).IsValueType)
        {
            if (ReferenceEquals(slot, component))
            {
                return;
            }

            // for reference types, if we know its a new class we process lifetime
            if (parent is { } entity)
            {
                Component<TComponent>.Destroyer?.Invoke(ref slot);
                slot = (TComponent)component;
                Component<TComponent>.Initer?.Invoke(entity, ref slot);
                return;
            }
        }

        // for value types (or when parent is null, so we dumbly set), treat modifications like writing a byref
        slot = (TComponent)component;
    }

    // copy behavior above
    internal sealed override void SetAt(Array buffer, Entity? parent, ComponentHandle component, int index)
    {
        ref TComponent slot = ref Index(buffer, index);
        if (!typeof(TComponent).IsValueType)
        {
            TComponent value = component.Retrieve<TComponent>();

            if (ReferenceEquals(slot, value))
            {
                return;
            }

            if (parent is { } entity)
            {
                Component<TComponent>.Destroyer?.Invoke(ref slot);
                slot = value;
                Component<TComponent>.Initer?.Invoke(entity, ref slot);
                return;
            }
        }

        slot = component.Retrieve<TComponent>();
    }

    internal sealed override void CallIniter(Array buffer, Entity parent, int index) => Component<TComponent>.Initer?.Invoke(parent, ref Index(buffer, index));
    internal sealed override void InvokeGenericActionWith(Array buffer, GenericEvent? action, Entity e, int index) => action?.Invoke(e, ref Index(buffer, index));
    internal sealed override void InvokeGenericActionWith(Array buffer, IGenericAction action, int index) => action?.Invoke(ref Index(buffer, index));
    internal sealed override void PullComponentFromAndClear(Array buffer, ComponentStorageRecord otherRunner, int me, int other, int otherRemoveIndex)
    {
        TComponent[] componentRunner = UnsafeExtensions.UnsafeCast<TComponent[]>(otherRunner.Buffer);

        ref var item = ref componentRunner.UnsafeArrayIndex(other);
        Index(buffer, me) = item;

        ref var downItem = ref componentRunner.UnsafeArrayIndex(otherRemoveIndex);
        item = downItem;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
        {
            downItem = default;
        }
    }

    internal sealed override void PullComponentFrom(Array buffer, IDTable storage, int me, int other)
    {
        ref var item = ref ((IDTable<TComponent>)storage).Buffer[other];
        Index(buffer, me) = item;
    }

    internal sealed override void Delete(Array buffer, DeleteComponentData data)
    {
        ref var from = ref Index(buffer, data.FromIndex);
        Component<TComponent>.Destroyer?.Invoke(ref from);
        Index(buffer, data.ToIndex) = from;


        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    internal sealed override ComponentHandle Store(Array buffer, int componentIndex)
    {
        ref var item = ref Index(buffer, componentIndex);

        //we can't just copy to stack and run the destroyer on it
        //it is stored
        Component<TComponent>.Destroyer?.Invoke(ref item);

        var handle = ComponentHandle.Create(item);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            item = default;

        return handle;
    }

    private static ref TComponent Index(Array buffer, int componentIndex)
    {
        return ref UnsafeExtensions.UnsafeCast<TComponent[]>(buffer).UnsafeArrayIndex(componentIndex);
    }
}