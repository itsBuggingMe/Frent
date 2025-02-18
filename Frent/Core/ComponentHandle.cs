namespace Frent.Core;
public struct ComponentHandle : IDisposable
{
    private int _index;
    private ComponentID _componentType;

    internal ComponentHandle(int index, ComponentID componentID)
    {
        _index = index;
        _componentType = componentID;
    }

    public static ComponentHandle Create<T>(in T comp)
    {
        return Component<T>.StoreComponent(comp);
    }

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

    public void Dispose() => Component.ComponentTable[_componentType.RawIndex].Storage.Consume(_index);

    public Type Type => _componentType.Type;
    public ComponentID ComponentID => _componentType;
    internal int Index => _index;
}
