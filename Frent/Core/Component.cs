using Frent.Collections;
using Frent.Components;
using Frent.Updating;
using Frent.Updating.Runners;

namespace Frent.Core;

public static class Component<T>
{
    static Component()
    {
        Component.ComponentSizes[typeof(T)] = PreformanceHelpers.GetSizeOfType<T>();
        ID = Component.GetComponentID(typeof(T));

        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out IComponentRunnerFactory? type))
        {
            if (type is IComponentRunnerFactory<T> casted)
            {
                RunnerInstance = casted;
                return;
            }

            throw new InvalidOperationException($"{typeof(T).FullName} is not initalized correctly. (Is the source generator working?)");
        }

        var fac = new NoneComponentRunnerFactory<T>();
        Component.NoneComponentRunnerTable[typeof(T)] = fac;
        RunnerInstance = fac;
    }

    public static readonly ComponentID ID;
    public static IComponentRunner<T> CreateInstance() => RunnerInstance.CreateStronglyTyped();

    private static readonly IComponentRunnerFactory<T> RunnerInstance;
}

public static class Component
{
    internal static FastStack<ComponentData> ComponentTable = FastStack<ComponentData>.Create(16);

    internal static Dictionary<Type, int> ComponentSizes = [];
    internal static Dictionary<Type, IComponentRunnerFactory> NoneComponentRunnerTable = [];

    private static Dictionary<Type, ComponentID> ExistingComponentIDs = [];
    private static int NextComponentID = -1;

    internal static IComponentRunner GetComponentRunnerFromType(Type t)
    {
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out IComponentRunnerFactory? type))
        {
            return (IComponentRunner)type.Create();
        }
        if (NoneComponentRunnerTable.TryGetValue(t, out type))
        {
            return (IComponentRunner)type.Create();
        }

        if (t.IsAssignableTo(typeof(IComponent)))
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
        //random size estimate of a managed type
        ComponentSizes[typeof(T)] = PreformanceHelpers.GetSizeOfType<T>();
        NoneComponentRunnerTable[typeof(T)] = new NoneComponentRunnerFactory<T>();
    }

    internal static ComponentID GetComponentID(Type t)
    {
        if (ExistingComponentIDs.TryGetValue(t, out ComponentID value))
        {
            return value;
        }

        //although this part is thread safe...
        //NOTHING ELSE IS YET!!!!
        ComponentID id = new ComponentID(Interlocked.Increment(ref NextComponentID));
        ExistingComponentIDs[t] = id;

        GlobalWorldTables.ModifyComponentTagTableIfNeeded(id.ID);

        if (ComponentSizes.TryGetValue(t, out int size))
        {
            ComponentTable.Push(new ComponentData(t, size));
        }
        else
        {
            //we give a estimate of 16 bytes?
            //ComponentTable.Push(new ComponentData(t, 16));
            throw new InvalidOperationException($"{t.FullName} is not initalized. (Did you initalize T with Component.RegisterComponent<T>()?)");
        }

        return id;
    }
}