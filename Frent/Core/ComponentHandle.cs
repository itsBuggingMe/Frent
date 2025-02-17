namespace Frent.Core;
public struct ComponentHandle
{
    private int _index;
    private ComponentID _componentType;

    internal ComponentHandle(int index, ComponentID componentID)
    {
        _index = index;
        _componentType = componentID;
    }

    public T Retrieve<T>()
    {
        if(_componentType != Component<T>.ID)
            FrentExceptions.Throw_InvalidOperationException("Wrong component handle type!");
        return Component<T>.GeneralComponentStorage.Take(_index);
    }

    public object RetrieveBoxed()
    {
        return Component.ComponentTable[_componentType.Index].Storage.TakeBoxed(_index);
    }

    public Type Type => _componentType.Type;
    public ComponentID ComponentID => _componentType;
    internal int Index => _index;
}
