using Frent.Collections;
using Frent.Components;
using System.Numerics;

namespace Frent.Core;

internal static class Component<T>
    where T : IComponent
{
    public static readonly int ID = Component.GetNextComponentID(typeof(T));
}

internal static class Component
{
    private static int NextComponentID = -1;
    internal static FastStack<Type> AllComponentTypesOrdered = FastStack<Type>.Create(16);


    internal static int GetNextComponentID(Type t)
    {
        int id = Interlocked.Increment(ref NextComponentID);
        AllComponentTypesOrdered.Push(t);

        ModifyComponentTable(t, id);

        return id;
    }

    private static void ModifyComponentTable(Type newType, int id)
    {
        var table = GlobalWorldTables.ComponentLocationTable;
        int componentTableLength = table[0].Length;
        
        //when adding a component, we only care about changing the length
        if(componentTableLength == id)
        {
            for (int i = 0; i < table.Length; i++)
            {
                ref var componentsForArchetype = ref table[i];
                Array.Resize(ref componentsForArchetype, componentTableLength << 1);
                componentsForArchetype.AsSpan(componentTableLength).Fill(byte.MaxValue);
            }
        }
    }
}