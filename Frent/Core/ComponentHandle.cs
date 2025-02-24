using Frent.Collections;
using Frent.Core.Events;
using System.Diagnostics.CodeAnalysis;

namespace Frent.Core;

public readonly struct ComponentHandle : IEquatable<ComponentHandle>, IDisposable
{
    private readonly int _index;
    private readonly ComponentID _componentType;


    internal ComponentHandle(int index, ComponentID componentID)
    {
        _index = index;
        _componentType = componentID;
    }

    public static ComponentHandle Create<T>(in T comp)
    {
        return Component<T>.StoreComponent(comp);
    }

    public static ComponentHandle CreateFromBoxed(ComponentID typeAs, object @object)
    {
        var index = Component.ComponentTable[typeAs.RawIndex].Storage.CreateBoxed(@object);
        return new ComponentHandle(index, typeAs);
    }

    public static ComponentHandle CreateFromBoxed(object @object) => CreateFromBoxed(Component.GetComponentID(@object.GetType()), @object);

    public T Retrieve<T>()
    {
        if(_componentType != Component<T>.ID)
            FrentExceptions.Throw_InvalidOperationException("Wrong component handle type!");
        return Component<T>.GeneralComponentStorage.Take(_index);
    }

    public object RetrieveBoxed()
    {
        return Component.ComponentTable[_componentType.RawIndex].Storage.TakeBoxed(_index);
    }

    public void InvokeComponentEventAndConsume(Entity entity, GenericEvent? @event)
    {
        Component.ComponentTable[_componentType.RawIndex].Storage.InvokeEventWithAndConsume(@event, entity, _index);
    }

    public void Dispose() => Component.ComponentTable[_componentType.RawIndex].Storage.Consume(_index);

    public bool Equals(ComponentHandle other) => other.ComponentID == ComponentID && other.Index == Index;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ComponentHandle handle && Equals(handle);

    public static bool operator ==(ComponentHandle left, ComponentHandle right) => left.Equals(right);
    public static bool operator !=(ComponentHandle left, ComponentHandle right) => !left.Equals(right);

    public Type Type => _componentType.Type;
    public ComponentID ComponentID => _componentType;
    internal int Index => _index;
    internal IDTable ParentTable => Component.ComponentTable[_componentType.RawIndex].Storage;
}
