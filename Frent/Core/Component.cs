﻿using Frent.Collections;
using Frent.Components;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
    private static readonly IComponentRunnerFactory<T> RunnerInstance;
    internal static readonly TrimmableStack<T> TrimmableStack;

    static Component()
    {
        (_id, TrimmableStack) = Component.GetExistingOrSetupNewComponent<T>();

        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out var type))
        {
            if (type.Factory is IComponentRunnerFactory<T> casted)
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
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out var type))
        {
            return (IComponentRunner)type.Factory.Create();
        }
        if (NoneComponentRunnerTable.TryGetValue(t, out var t1))
        {
            return (IComponentRunner)t1.Create();
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

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.ID);

            TrimmableStack<T> stack = new TrimmableStack<T>();
            ComponentTable.Push(new ComponentData(type, stack, GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out var order) ? order.UpdateOrder : 0));

            return (id, stack);
        }
    }

    /// <summary>
    /// Gets the component ID of a type
    /// </summary>
    /// <param name="t">The type to get the component ID of</param>
    /// <returns>The component ID</returns>
    public static ComponentID GetComponentID(Type t)
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

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.ID);

            ComponentTable.Push(new ComponentData(t, GetTrimmableStack(t), GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out var order) ? order.UpdateOrder : 0));

            return id;
        }
    }

    private static TrimmableStack GetTrimmableStack(Type type)
    {
        if (NoneComponentRunnerTable.TryGetValue(type, out var fac))
            return (TrimmableStack)fac.CreateStack();
        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out var data))
            return (TrimmableStack)data.Factory.CreateStack();
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