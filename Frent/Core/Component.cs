using Frent.Collections;
using Frent.Components;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Diagnostics.CodeAnalysis;

namespace Frent.Core;

/// <summary>
/// Used to quickly get the component ID of a given type
/// </summary>
/// <typeparam name="T">The type of component</typeparam>
public static class Component<T>
{
    /// <summary>
    /// The component ID for <typeparamref name="T"/>
    /// </summary>
    public static ComponentID ID => _id;

    private static readonly ComponentID _id;

    internal static readonly TrimmableStack<T> TrimmableStack;

    static Component()
    {
        (_id, TrimmableStack) = Component.GetExistingOrSetupNewComponent<T>();

        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out IComponentRunnerFactory? type))
        {
            if (type is IComponentRunnerFactory<T> casted)
            {
                RunnerInstance = casted;
                return;
            }

            throw new InvalidOperationException($"{typeof(T).FullName} is not initalized correctly. (Is the source generator working?)");
        }

        var fac = new NoneUpdateRunnerFactory<T>();
        Component.NoneComponentRunnerTable[typeof(T)] = fac;
        RunnerInstance = fac;
    }

    internal static IComponentRunner<T> CreateInstance() => RunnerInstance.CreateStronglyTyped();

    private static readonly IComponentRunnerFactory<T> RunnerInstance;
}

/// <summary>
/// Class for registering components
/// </summary>
public static class Component
{
    internal static FastStack<ComponentData> ComponentTable = FastStack<ComponentData>.Create(16);

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

        Throw_ComponentTypeNotInit(t);
        return null!;
    }

    /// <summary>
    /// Register components with this method to be able to use them programmically. Note that components that implement an IComponent interface do not need to be registered
    /// </summary>
    /// <typeparam name="T">The type of component to implement</typeparam>
    public static void RegisterComponent<T>()
    {
        if (!GenerationServices.UserGeneratedTypeMap.ContainsKey(typeof(T)))
            NoneComponentRunnerTable[typeof(T)] = new NoneUpdateRunnerFactory<T>();
    }

    internal static (ComponentID ComponentID, TrimmableStack<T> Stack) GetExistingOrSetupNewComponent<T>()
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            var type = typeof(T);
            if (ExistingComponentIDs.TryGetValue(type, out ComponentID value))
            {
                return (value, (TrimmableStack<T>)ComponentTable[value.ID].Stack);
            }

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[type] = id;

            GlobalWorldTables.ModifyComponentTagTableIfNeeded(id.ID);

            TrimmableStack<T> stack = new TrimmableStack<T>();
            ComponentTable.Push(new ComponentData(type, stack));

            return (id, stack);
        }
    }

    internal static ComponentID GetComponentID(Type t)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            if (ExistingComponentIDs.TryGetValue(t, out ComponentID value))
            {
                return value;
            }

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[t] = id;

            GlobalWorldTables.ModifyComponentTagTableIfNeeded(id.ID);

            ComponentTable.Push(new ComponentData(t, GetTrimmableStack(t)));

            return id;
        }
    }

    private static TrimmableStack GetTrimmableStack(Type type)
    {
        if (NoneComponentRunnerTable.TryGetValue(type, out var fac))
            return (TrimmableStack)fac.CreateStack();
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out fac))
            return (TrimmableStack)fac.CreateStack();
        if (type == typeof(void))
            return null!;
        Throw_ComponentTypeNotInit(type);
        return null!;
    }

    [DoesNotReturn]
    private static void Throw_ComponentTypeNotInit(Type t)
    {
        if (t.IsAssignableTo(typeof(IComponentBase)))
        {
            throw new InvalidOperationException($"{t.FullName} is not initalized. (Is the source generator working?)");
        }
        else
        {
            throw new InvalidOperationException($"{t.FullName} is not initalized. (Did you initalize T with Component.RegisterComponent<T>()?)");
        }
    }

    //initalize default(ComponentID) to point to void
    static Component() => GetComponentID(typeof(void));
}