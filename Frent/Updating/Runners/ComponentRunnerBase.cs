﻿using Frent.Buffers;
using Frent.Collections;
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
    public void Trim(int index) => ResizeBuffer((int)BitOperations.RoundUpToPowerOf2((uint)index));
    //TODO: pool
    public void ResizeBuffer(int size) => ResizeBuffer(size);
    //Note - no unsafe here
    public void SetAt(object component, int index) => this[index] = (TComponent)component;
    public object GetAt(int index) => this[index]!;
    public void InvokeGenericActionWith(GenericEvent? action, Entity e, int index) => action?.Invoke(e, ref this[index]);
    public void InvokeGenericActionWith(IGenericAction action, int index) => action?.Invoke(ref this[index]);
    public ComponentID ComponentID => Component<TComponent>.ID;

    public void PullComponentFrom(IComponentRunner otherRunner, EntityLocation me, EntityLocation other)
    {
        ComponentStorage<TComponent> componentRunner = UnsafeExtensions.UnsafeCast<ComponentStorage<TComponent>>(otherRunner);
        this[me.Index] = componentRunner[other.Index];
    }

    public void PullComponentFrom(TrimmableStack storage, EntityLocation me, int other) => this[me.Index] = ((TrimmableStack<TComponent>)storage).StrongBuffer[other];

    public void Delete(DeleteComponentData data)
    {
        ref var from = ref this[data.FromIndex];
        this[data.ToIndex] = from;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            from = default;
    }

    public TrimmableStack PushComponentToStack(int componentIndex, out int stackIndex)
    {
        Component<TComponent>.TrimmableStack.PushStronglyTyped(this[componentIndex], out stackIndex);
        return Component<TComponent>.TrimmableStack;
    }
}

internal unsafe abstract class ComponentStorage<TComponent> : IDisposable
{
    public ref TComponent this[int index]
    {
        get
        {
            if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
            {
                return ref _managed!.UnsafeArrayIndex(index);
            }

            Debug.Assert(index >= 0 && index <_nativeLength);

            return ref _native[index];
        }
    }

    public ComponentStorage()
    {
        if(RuntimeHelpers.IsReferenceOrContainsReferences<TComponent>())
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

    private void Resize(int size)
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

    public void Dispose()
    {
        _nativeArray.Dispose();
    }
}