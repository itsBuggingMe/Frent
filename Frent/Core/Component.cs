using Frent.Collections;
using Frent.Components;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent.Core;

/// <summary>
/// Used to quickly get the component ID of a given type
/// </summary>
/// <typeparam name="T">The type of component</typeparam>
public static class Component<T>
{
    internal static ComponentStorageRecord CreateInstance(int capacity) => new ComponentStorageRecord(capacity == 0 ? [] : new T[capacity], BufferManagerInstance);

    /// <summary>
    /// The component ID for <typeparamref name="T"/>
    /// </summary>
    public static ComponentID ID => _id;

    private static readonly ComponentID _id;
    internal static readonly IDTable<T> GeneralComponentStorage;
    internal static readonly ComponentDelegates<T>.InitDelegate? Initer;
    internal static readonly ComponentDelegates<T>.DestroyDelegate? Destroyer;

    //unroll + devirt until > 4 components
    internal static readonly int UpdateMethodCount;
    internal static readonly IRunner? s_r0;
    internal static readonly IRunner? s_r1;
    internal static readonly IRunner? s_r2;
    internal static readonly IRunner? s_r3;
    internal static readonly IRunner[]? _overflow;
    internal static readonly ComponentBufferManager BufferManagerInstance;

    internal static readonly bool IsDestroyable = typeof(T).IsValueType ? default(T) is IDestroyable : typeof(IDestroyable).IsAssignableFrom(typeof(T));
    
    internal static void UpdateComponentBuffer(Array array, Archetype archetype, World world)
    {
        if (UpdateMethodCount <= 4)
        {
            switch(UpdateMethodCount)
            {
                case 0: break;
                case 1: 
                    s_r0!.Run(array, archetype, world);
                    break;
                case 2:
                    s_r0!.Run(array, archetype, world);
                    s_r1!.Run(array, archetype, world);
                    break;
                case 3:
                    s_r0!.Run(array, archetype, world);
                    s_r1!.Run(array, archetype, world);
                    s_r2!.Run(array, archetype, world);
                    break;
                case 4:
                    s_r0!.Run(array, archetype, world);
                    s_r1!.Run(array, archetype, world);
                    s_r2!.Run(array, archetype, world);
                    s_r3!.Run(array, archetype, world);
                    break;
            }
        }
        else
        {
            foreach (var runner in _overflow!)
            {
                runner.Run(array, archetype, world);
            }
        }
    }

    internal static void UpdateComponentBuffer(Array array, Archetype archetype, World world, int start, int count)
    {
        if (UpdateMethodCount <= 4)
        {
            switch (UpdateMethodCount)
            {
                case 0: break;
                case 1:
                    s_r0!.Run(array, archetype, world, start, count);
                    break;
                case 2:
                    s_r0!.Run(array, archetype, world, start, count);
                    s_r1!.Run(array, archetype, world, start, count);
                    break;
                case 3:
                    s_r0!.Run(array, archetype, world, start, count);
                    s_r1!.Run(array, archetype, world, start, count);
                    s_r2!.Run(array, archetype, world, start, count);
                    break;
                case 4:
                    s_r0!.Run(array, archetype, world, start, count);
                    s_r1!.Run(array, archetype, world, start, count);
                    s_r2!.Run(array, archetype, world, start, count);
                    s_r3!.Run(array, archetype, world, start, count);
                    break;
            }
        }
        else
        {
            foreach (var runner in _overflow!)
            {
                runner.Run(array, archetype, world, start, count);
            }
        }
    }

    static Component()
    {
        (_id, GeneralComponentStorage, Initer, Destroyer, BufferManagerInstance) = Component.GetExistingOrSetupNewComponent<T>();

        if(GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out var runners))
        {
            UpdateMethodCount = runners.Length;
            if(runners.Length <= 4)
            {
                for(int i = 0; i < runners.Length; i++)
                {
                    switch(i)
                    {
                        case 0: s_r0 = runners[0]; break;
                        case 1: s_r1 = runners[1]; break;
                        case 2: s_r2 = runners[2]; break;
                        case 3: s_r3 = runners[3]; break;
                        default: throw new Exception("???");
                    }
                }
            }
            else
            {
                _overflow = runners;
            }
        }
        else
        {
            UpdateMethodCount = 0;
        }
    }
}

/// <summary>
/// Used only in source generation
/// </summary>
public static class ComponentDelegates<T>
{
    /// <summary>
    /// Used only in source generation
    /// </summary>
    public delegate void InitDelegate(Entity entity, ref T component);
    /// <summary>
    /// Used only in source generation
    /// </summary>
    public delegate void DestroyDelegate(ref T component);
}

/// <summary>
/// Class for registering components
/// </summary>
public static class Component
{
    internal static FastStack<ComponentData> ComponentTable = FastStack<ComponentData>.Create(16);

    private static Dictionary<Type, ComponentID> ExistingComponentIDs = [];

    private static int NextComponentID = -1;

    internal static ComponentBufferManager GetComponentFactoryFromType(Type t)
    {
        if (!GenerationServices.ComponentFactories.TryGetValue(t, out var factory))
            Throw_ComponentTypeNotInit(t);

        return factory;
    }

    /// <summary>
    /// Register components with this method to be able to use them programmically. Note that components that implement an IComponent interface do not need to be registered
    /// </summary>
    /// <typeparam name="T">The type of component to implement</typeparam>
    public static void RegisterComponent<T>()
    {
        GenerationServices.RegisterComponent<T>();
    }

    internal static (ComponentID ComponentID, IDTable<T> Stack, ComponentDelegates<T>.InitDelegate? Initer, ComponentDelegates<T>.DestroyDelegate? Destroyer, ComponentBufferManager BufferManager) GetExistingOrSetupNewComponent<T>()
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            var type = typeof(T);
            if (ExistingComponentIDs.TryGetValue(type, out ComponentID componentID))
            {
                return 
                    (
                        componentID, 
                        (IDTable<T>)ComponentTable[componentID.RawIndex].Storage, 
                        (ComponentDelegates<T>.InitDelegate?)ComponentTable[componentID.RawIndex].Initer, 
                        (ComponentDelegates<T>.DestroyDelegate?)ComponentTable[componentID.RawIndex].Destroyer, 
                        GetComponentFactoryFromType(typeof(T))
                    );
            }

            EnsureTypeInit(type);

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[type] = id;

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.RawIndex);

            var initDelegate = (ComponentDelegates<T>.InitDelegate?)(GenerationServices.TypeIniters.TryGetValue(type, out var v) ? v : null);
            var destroyDelegate = (ComponentDelegates<T>.DestroyDelegate?)(GenerationServices.TypeDestroyers.TryGetValue(type, out var v2) ? v2 : null);

            IDTable<T> stack = new IDTable<T>();
            ComponentTable.Push(new ComponentData(type, stack,
                GenerationServices.TypeIniters.TryGetValue(type, out var v1) ? initDelegate : null,
                GenerationServices.TypeDestroyers.TryGetValue(type, out var d) ? destroyDelegate : null));

            return (id, stack, initDelegate, destroyDelegate, GetComponentFactoryFromType(typeof(T)));
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

            EnsureTypeInit(t);

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[t] = id;

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.RawIndex);

            ComponentTable.Push(new ComponentData(t, CreateComponentTable(t),
                GenerationServices.TypeIniters.TryGetValue(t, out var v) ? v : null,
                GenerationServices.TypeDestroyers.TryGetValue(t, out var d) ? d : null));

            return id;
        }
    }

    private static void EnsureTypeInit(Type t)
    {
        if (GenerationServices.UserGeneratedTypeMap.ContainsKey(t))
            return;
        if (!typeof(IComponentBase).IsAssignableFrom(t))
            return;
        //it needs init!!
#if NETSTANDARD2_1
        t.TypeInitializer?.Invoke(null, []);
#else
        RuntimeHelpers.RunClassConstructor(t.TypeHandle);
#endif
    }

    private static IDTable CreateComponentTable(Type type)
    {
        if (GenerationServices.ComponentFactories.TryGetValue(type, out ComponentBufferManager? factory))
            return factory.CreateTable();
        if (type == typeof(void))
            return null!;
        Throw_ComponentTypeNotInit(type);
        return null!;
    }

    [DoesNotReturn]
    private static void Throw_ComponentTypeNotInit(Type t)
    {
        if (typeof(IComponentBase).IsAssignableFrom(t))
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