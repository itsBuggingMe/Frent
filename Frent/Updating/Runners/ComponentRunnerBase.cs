using Frent.Buffers;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Updating.Runners;

internal abstract class ComponentRunnerBase<TSelf, TComponent> : ComponentStorage<TComponent>, IComponentRunner<TComponent>
    where TSelf : IComponentRunner<TComponent>, new()
{
    public abstract void Run(World world, Archetype b);
    public abstract void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    //TODO: improve
    public void Trim(int index) => Array.Resize(ref _components, (int)BitOperations.RoundUpToPowerOf2((uint)index));
    //TODO: pool
    public void ResizeBuffer(int size) => Array.Resize(ref _components, size);
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

internal abstract class ComponentStorage<TComponent>
{
    public ref TComponent this[int index]
    {
        get => ref _components.UnsafeArrayIndex(index);
    }
    internal TComponent[] Components => _components;
    protected TComponent[] _components = new TComponent[1];
    public Span<TComponent> AsSpan() => _components.AsSpan();
}