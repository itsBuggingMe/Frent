using Frent.Collections;
using Frent.Components;
using Frent.Core.Structures;
using Frent.Updating;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

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
    internal static readonly IDTable<T> GeneralComponentStorage;
    internal static readonly ComponentDelegates<T>.InitDelegate? Initer;
    internal static readonly ComponentDelegates<T>.DestroyDelegate? Destroyer;
    internal static readonly JsonTypeInfo<T>? DefaultJsonTypeInfo; // TODO: init
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

    internal static void InitalizeComponentRunnerImpl(ComponentStorageRecord[] runners, ComponentStorageRecord[] tmpStorages, byte[] map)
    {
        if (!IsSparseComponent)
        {
            int i = map.UnsafeArrayIndex(ID.RawIndex) & GlobalWorldTables.IndexBits;
            runners[i] = new ComponentStorageRecord(new T[1], BufferManagerInstance);
            tmpStorages[i] = new ComponentStorageRecord(Array.Empty<T>(), BufferManagerInstance);
        }
    }

    internal static readonly int SparseSetComponentIndex;

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

        if(Component.CachedComponentFactories.TryGetValue(typeof(T), out var componentBufferManager))
        {
            BufferManagerInstance = (ComponentBufferManager<T>)componentBufferManager;
        }
        else
        {
            Component.CachedComponentFactories[typeof(T)] = BufferManagerInstance = new ComponentBufferManager<T>();
        }

        (_id, GeneralComponentStorage, Initer, Destroyer, SparseSetComponentIndex) = Component.GetExistingOrSetupNewComponent<T>();

        if (GenerationServices.UserGeneratedTypeMap.TryGetValue(typeof(T), out var runners))
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
    internal static FastStack<SparseComponentData> ComponentTableBySparseIndex = FastStack<SparseComponentData>.Create(2);

    private static Dictionary<Type, ComponentID> ExistingComponentIDs = [];

    internal static readonly Dictionary<Type, ComponentBufferManager> CachedComponentFactories = [];

    private static int NextComponentID = -1;
    private static int NextSparseSetComponentIndex = 0;

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

    internal static (ComponentID ComponentID, IDTable<T> Stack, ComponentDelegates<T>.InitDelegate? Initer, ComponentDelegates<T>.DestroyDelegate? Destroyer, int SparseIndex) GetExistingOrSetupNewComponent<T>()
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
                        ComponentTable[componentID.RawIndex].SparseComponentIndex
                    );
            }

            EnsureTypeInit(type);

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[type] = id;

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.RawIndex);

            var initDelegate = (ComponentDelegates<T>.InitDelegate?)(GenerationServices.TypeIniters.GetValueOrDefault(type));
            var destroyDelegate = (ComponentDelegates<T>.DestroyDelegate?)(GenerationServices.TypeDestroyers.GetValueOrDefault(type));
            int sparseIndex = Component<T>.IsSparseComponent ? ++NextSparseSetComponentIndex : 0;

            IDTable<T> stack = new IDTable<T>();
            var data = new ComponentData(type, stack, GetComponentFactoryFromType(type),
                initDelegate,
                destroyDelegate,
                GenerationServices.UserGeneratedTypeMap.GetValueOrDefault(type) ?? [],
                sparseIndex
            );
            ComponentTable.Push(data);

            if (sparseIndex != 0)
            {
                GlobalWorldTables.RegisterNewSparseSetComponent(sparseIndex, CachedComponentFactories[type]);
                ComponentTableBySparseIndex.Push(new(data.Factory, sparseIndex, id));
            }

            return (id, stack, initDelegate, destroyDelegate, sparseIndex);
        }
    }

    /// <summary>
    /// Gets the component ID of a type
    /// </summary>
    /// <param name="t">The type to get the component ID of</param>
    /// <returns>The component ID</returns>
    public static ComponentID GetComponentID(Type t) => GetComponentIDCore(t, null);

    private static ComponentID GetComponentIDCore(Type type, IDTable? table)
    {
        lock (GlobalWorldTables.BufferChangeLock)
        {
            if (ExistingComponentIDs.TryGetValue(type, out ComponentID value))
            {
                return value;
            }

            EnsureTypeInit(type);

            int nextIDInt = ++NextComponentID;

            if (nextIDInt == ushort.MaxValue)
                throw new InvalidOperationException($"Exceeded maximum unique component type count of 65535");

            ComponentID id = new ComponentID((ushort)nextIDInt);
            ExistingComponentIDs[type] = id;

            GlobalWorldTables.GrowComponentTagTableIfNeeded(id.RawIndex);

            bool isSparseComponent = typeof(ISparseComponent).IsAssignableFrom(type);
            int sparseIndex = isSparseComponent ? ++NextSparseSetComponentIndex : 0;
            ComponentData data = new ComponentData(type, table ?? CreateComponentTable(type), type == typeof(void) ? null! : GetComponentFactoryFromType(type),
                GenerationServices.TypeIniters.GetValueOrDefault(type),
                GenerationServices.TypeDestroyers.GetValueOrDefault(type),
                GenerationServices.UserGeneratedTypeMap.TryGetValue(type, out var m) ? m : [],
                sparseIndex
            );

            ComponentTable.Push(data);
            if (isSparseComponent)
            {
                GlobalWorldTables.RegisterNewSparseSetComponent(sparseIndex, CachedComponentFactories[type]);
                ComponentTableBySparseIndex.Push(new(data.Factory, data.SparseComponentIndex, id));
            }

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
    static Component()
    {
        GetComponentID(typeof(void));
        // offset by one
        ComponentTableBySparseIndex.Push(default);
    }
}