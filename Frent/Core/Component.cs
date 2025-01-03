using Frent.Collections;
using Frent.Updating;
using Frent.Components;
using Frent.Updating.Runners;

namespace Frent.Core;

internal static class Component<T>
{
    public static readonly int ID = Component.GetComponentID(typeof(T));
    public static IComponentRunner<T> CreateInstance() => RunnerInstance.CloneStronglyTyped();

    private static readonly IComponentRunner<T> RunnerInstance;
    static Component()
    {
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out IComponentRunner? type))
        {
            if (type is IComponentRunner<T> casted)
            {
                RunnerInstance = casted;
                return;
            }

            throw new InvalidOperationException($"{typeof(T).FullName} is not initalized. (Is the source generator working?)");
        }

        Component.NoneComponentRunnerTable[typeof(T)] = RunnerInstance = new None<T>();
    }
}

public static class Component
{
    internal static int ComponentTableBufferSize { get; private set; }
    internal static FastStack<Type> AllComponentTypesOrdered = FastStack<Type>.Create(16);
    private static int NextComponentID = -1;
    private static Dictionary<Type, int> ExistingComponentIDs = [];

    internal static Dictionary<Type, IComponentRunner> NoneComponentRunnerTable = [];
    internal static IComponentRunner GetComponentRunnerFromType(Type t)
    {
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out IComponentRunner? type))
        {
            return type.Clone();
        }
        if (NoneComponentRunnerTable.TryGetValue(t, out type))
        {
            return type.Clone();
        }

        if(t.IsAssignableTo(typeof(IComponent)))
        {
            throw new InvalidOperationException($"{t.FullName} is not initalized. (Is the source generator working?)");
        }
        else
        {
            throw new InvalidOperationException($"{t.FullName} is not initalized. (Did you initalize T with Component.RegisterComponent<T>()?)");
        }
    }

    public static void RegisterComponent<T>()
    {
        NoneComponentRunnerTable[typeof(T)] = new None<T>();
    }
    internal static int GetComponentID(Type t)
    {
        if (ExistingComponentIDs.TryGetValue(t, out int value))
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
        if (componentTableLength == id)
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