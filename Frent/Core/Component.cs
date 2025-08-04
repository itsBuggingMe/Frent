using Frent.Collections;
using Frent.Components;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.ObjectModel;
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
    private static readonly bool _isSparseComponentAndReference;

    internal static bool IsSparseComponent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(typeof(T).IsValueType)
            {
                return default(T) is ISparseComponent;
            }

            return _isSparseComponentAndReference;
        }
    }

    internal static /*readonly*/ nint SparseSetComponentIndex => throw new NotImplementedException();

    internal static readonly UpdateMethodData[] UpdateMethods;
    internal static readonly ComponentBufferManager<T> BufferManagerInstance;

    internal static readonly bool IsDestroyable = typeof(T).IsValueType ? default(T) is IDestroyable : typeof(IDestroyable).IsAssignableFrom(typeof(T));

    static Component()
    {
        if (!typeof(T).IsValueType)
        {
            //this field is used in Component.GetExistingOrSetupNewComponent<T>()
            _isSparseComponentAndReference = typeof(ISparseComponent).IsAssignableFrom(typeof(T));
        }


        (_id, GeneralComponentStorage, Initer, Destroyer) = Component.GetExistingOrSetupNewComponent<T>();

        if(Component.CachedComponentFactories.TryGetValue(typeof(T), out var componentBufferManager))
        {
            BufferManagerInstance = (ComponentBufferManager<T>)componentBufferManager;
        }
        else
        {
            Component.CachedComponentFactories[typeof(T)] = BufferManagerInstance = new ComponentBufferManager<T>();
        }


        if(GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out var runners))
        {
            UpdateMethods = runners;
        }
        else
        {
            UpdateMethods = [];
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

    internal static readonly Dictionary<Type, ComponentBufferManager> CachedComponentFactories = [];

    private static int NextComponentID = -1;

    internal static ComponentBufferManager GetComponentFactoryFromType(Type t)
    {
        if (!CachedComponentFactories.TryGetValue(t, out var factory))
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

    internal static (ComponentID ComponentID, IDTable<T> Stack, ComponentDelegates<T>.InitDelegate? Initer, ComponentDelegates<T>.DestroyDelegate? Destroyer) GetExistingOrSetupNewComponent<T>()
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
                        (ComponentDelegates<T>.DestroyDelegate?)ComponentTable[componentID.RawIndex].Destroyer
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
                GenerationServices.TypeDestroyers.TryGetValue(type, out var d) ? destroyDelegate : null,
                GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out var m) ? m : [],
                Component<T>.IsSparseComponent));

            return (id, stack, initDelegate, destroyDelegate);
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
                GenerationServices.TypeDestroyers.TryGetValue(t, out var d) ? d : null,
                GenerationServices.UserGeneratedTypeMap.TryGetValue(t, out var m) ? m : [],
                typeof(ISparseComponent).IsAssignableFrom(t)
                ));

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
        if (CachedComponentFactories.TryGetValue(type, out ComponentBufferManager? factory))
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