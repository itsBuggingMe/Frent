using Frent.Collections;
using Frent.Components;
using System.Numerics;

namespace Frent.Core;

internal static class Component<T>
{
    public static readonly int ID = Component.GetComponentID(typeof(T));
}

internal static class Component
{
    internal static int ComponentTableBufferSize { get; private set; }
    internal static FastStack<Type> AllComponentTypesOrdered = FastStack<Type>.Create(16);
    private static int NextComponentID = -1;
    private static Dictionary<Type, int> ExistingComponentIDs = [];

    public static int GetComponentID(Type t)
    {
        if(ExistingComponentIDs.TryGetValue(t, out int value))
        {
            return value;
        }

        //although this part is thread safe...
        //NOTHING ELSE IS YET!!!!
        int id = Interlocked.Increment(ref NextComponentID);
        ExistingComponentIDs[t] = id;

        ModifyComponentTable(t, id);

        AllComponentTypesOrdered.Push(t);

        return id;
    }

    private static void ModifyComponentTable(Type newType, int id)
    {
        var table = GlobalWorldTables.ComponentLocationTable;
        int componentTableLength = AllComponentTypesOrdered.Count;
        
        //when adding a component, we only care about changing the length
        if(componentTableLength == id)
        {
            ComponentTableBufferSize = Math.Max(componentTableLength << 1, 1);
            for (int i = 0; i < table.Length; i++)
            {
                ref var componentsForArchetype = ref table[i];
                Array.Resize(ref componentsForArchetype, ComponentTableBufferSize);
                componentsForArchetype.AsSpan(componentTableLength).Fill(byte.MaxValue);
            }
        }
    }
}