namespace Frent.Core;

public readonly struct ComponentID(int id)
{
    internal readonly int ID = id;
    public Type Type => Component.ComponentTable[ID].Type;
}
