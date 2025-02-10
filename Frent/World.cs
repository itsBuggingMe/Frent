using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Systems;
using Frent.Updating;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Frent.Tests")]
namespace Frent;

/// <summary>
/// A collection of entities that can be updated and queried.
/// </summary>
public partial class World : IDisposable
{
    #region Static Version Management
    private static FastStack<(byte ID, byte Version)> _recycledWorldIDs = FastStack<(byte ID, byte Version)>.Create(16);
    private static byte _nextWorldID;
    #endregion

    internal static readonly ushort DefaultWorldCachePackedValue = new Entity(byte.MaxValue, byte.MaxValue, 0, 0).PackedWorldInfo;

    //the idea is what work is usually done at one world at a time
    //we can save a lookup and a deref by storing it statically
    //this saves a few nanoseconds and makes the public entity apis 2x as fast
    internal static World? QuickWorldCache;
    internal static ushort WorldCachePackedValue = DefaultWorldCachePackedValue;

    internal NativeTable<EntityLookup> EntityTable = new NativeTable<EntityLookup>(32);
    internal Archetype[] WorldArchetypeTable;
    internal Dictionary<ArchetypeEdgeKey, Archetype> ArchetypeGraphEdges = [];

    private NativeStack<EntityIDOnly> _recycledEntityIds = new NativeStack<EntityIDOnly>(8);
    private Dictionary<Type, (FastStack<ComponentID> Stack, int NextComponentIndex)> _updatesByAttributes = [];
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;
    internal readonly ushort PackedIDVersion;
    internal readonly Entity DefaultWorldEntity;
    private bool _isDisposed = false;

    internal Dictionary<int, Query> QueryCache = [];

    internal CountdownEvent SharedCountdown => _sharedCountdown;
    private CountdownEvent _sharedCountdown = new(0);
    private FastStack<ArchetypeID> _enabledArchetypes = FastStack<ArchetypeID>.Create(16);

    private volatile int _allowStructuralChanges;

    internal CommandBuffer WorldUpdateCommandBuffer;

    internal EntityOnlyEvent EntityCreatedEvent = new EntityOnlyEvent();
    internal EntityOnlyEvent EntityDeletedEvent = new EntityOnlyEvent();
    internal Event<ComponentID> ComponentAddedEvent = new Event<ComponentID>();
    internal Event<ComponentID> ComponentRemovedEvent = new Event<ComponentID>();
    internal TagEvent Tagged = new TagEvent();
    internal TagEvent Detached = new TagEvent();

    internal EntityFlags WorldEventFlags; 
    internal FastLookup CompAddLookup = new();
    internal FastLookup CompRemoveLookup = new();
    internal FastLookup TagAddLookup = new();
    internal FastLookup TagRemoveLookup = new();

    /// <summary>
    /// Invoked whenever an entity is created on this world.
    /// </summary>
    public event Action<Entity> EntityCreated 
    { 
        add
        {
            EntityCreatedEvent.Add(value);
            WorldEventFlags |= EntityFlags.WorldCreate;
        } 
        remove
        {
            EntityCreatedEvent.Remove(value);
            if(!EntityCreatedEvent.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldCreate;
        }
    }
    /// <summary>
    /// Invoked whenever an entity belonging to this world is deleted.
    /// </summary>
    public event Action<Entity> EntityDeleted
    { 
        add
        {
            EntityDeletedEvent.Add(value);
            WorldEventFlags |= EntityFlags.WorldOnDelete;
        } 
        remove
        {
            EntityDeletedEvent.Remove(value);
            if(!EntityDeletedEvent.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldOnDelete;
        }
    }

    /// <summary>
    /// Invoked whenever a component is added to an entity.
    /// </summary>
    public event Action<Entity, ComponentID> ComponentAdded
    {
        add
        {
            ComponentAddedEvent.Add(value);
            WorldEventFlags |= EntityFlags.WorldAddComp;
        } 
        remove
        {
            ComponentAddedEvent.Remove(value);
            if(!ComponentAddedEvent.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldAddComp;
        }
    }

    /// <summary>
    /// Invoked whenever a component is removed from an entity.
    /// </summary>
    public event Action<Entity, ComponentID> ComponentRemoved
    {
        add
        {
            ComponentRemovedEvent.Add(value);
            WorldEventFlags |= EntityFlags.WorldRemoveComp;
        } 
        remove
        {
            ComponentRemovedEvent.Remove(value);
            if(!ComponentRemovedEvent.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldRemoveComp;
        }
    }

    /// <summary>
    /// Invoked whenever a tag is added to an entity.
    /// </summary>
    public event Action<Entity, TagID> TagTagged
    {
        add
        {
            Tagged.Add(value);
            WorldEventFlags |= EntityFlags.WorldTagged;
        } 
        remove
        {
            Tagged.Remove(value);
            if(!Tagged.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldTagged;
        }
    }

    /// <summary>
    /// Invoked whenever a tag is removed from an entity.
    /// </summary>
    public event Action<Entity, TagID> TagDetached
    {
        add
        {
            Detached.Add(value);
            WorldEventFlags |= EntityFlags.WorldDetach;
        } 
        remove
        {
            Detached.Remove(value);
            if(!Detached.HasListeners)
                WorldEventFlags &= ~EntityFlags.WorldDetach;
        }
    }

    internal Dictionary<EntityIDOnly, EventRecord> EventLookup = [];

    /// <summary>
    /// The current uniform provider used when updating components/queries with uniforms.
    /// </summary>
    public IUniformProvider UniformProvider 
    { 
        get => _uniformProvider; 
        set => _uniformProvider = value ?? NullUniformProvider.Instance; 
    }
    private IUniformProvider _uniformProvider;

    /// <summary>
    /// Gets the current number of entities managed by the world.
    /// </summary>
    public int EntityCount => _nextEntityID - _recycledEntityIds.Count;

    /// <summary>
    /// The current world config.
    /// </summary>
    public Config CurrentConfig { get; set; }

    /// <summary>
    /// Creates a world with zero entities and a uniform provider.
    /// </summary>
    /// <param name="uniformProvider">The initial uniform provider to be used.</param>
    /// <param name="config">The inital config to use. If not provided, <see cref="Config.Singlethreaded"/> is used.</param>
    public World(IUniformProvider? uniformProvider = null, Config? config = null)
    {
        CurrentConfig = config ?? Config.Singlethreaded;
        _uniformProvider = uniformProvider ?? NullUniformProvider.Instance;
        (ID, Version) = _recycledWorldIDs.TryPop(out var id) ? id : (_nextWorldID++, byte.MaxValue);
        IDAsUInt = ID;

        if (_nextWorldID == byte.MaxValue)
            throw new Exception("Max world count reached");

        GlobalWorldTables.Worlds[ID] = this;

        WorldArchetypeTable = new Archetype[GlobalWorldTables.ComponentTagLocationTable.Length];
        PackedIDVersion = new Entity(ID, Version, 0, 0).PackedWorldInfo;

        WorldUpdateCommandBuffer = new CommandBuffer(this);
        DefaultWorldEntity = new Entity(ID, Version, default, default);
    }

    internal Entity CreateEntityFromLocation(EntityLocation entityLocation)
    {
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, (ushort)0);
        EntityTable[id] = new(entityLocation, version);
        return new Entity(ID, Version, version, id);
    }

    /// <summary>
    /// Updates all component instances in the world that implement a component interface, e.g., <see cref="IComponent"/>
    /// </summary>
    public void Update()
    {
        EnterDisallowState();
        try
        {
            if (CurrentConfig.MultiThreadedUpdate)
            {
                foreach (var element in _enabledArchetypes.AsSpan())
                {
                    element.Archetype(this).MultiThreadedUpdate(_sharedCountdown, this);
                }
            }
            else
            {
                foreach (var element in _enabledArchetypes.AsSpan())
                {
                    element.Archetype(this).Update(this);
                }
            }
        }
        finally
        {
            ExitDisallowState();
        }
    }

    /// <summary>
    /// Updates all component instances in the world that implement a component interface and have update methods with the <typeparamref name="T"/> attribute
    /// </summary>
    /// <typeparam name="T">The type of attribute to filter</typeparam>
    public void Update<T>() where T : UpdateTypeAttribute => Update(typeof(T));

    /// <summary>
    /// Updates all component instances in the world that implement a component interface and have update methods with an attribute of type <paramref name="attributeType"/>
    /// </summary>
    /// <param name="attributeType">The attribute type to filter</param>
    public void Update(Type attributeType)
    {
        EnterDisallowState();

        try
        {
            ref var appliesTo = ref CollectionsMarshal.
                GetValueRefOrAddDefault(_updatesByAttributes, attributeType, out bool exists);
            if (!exists)
            {
                appliesTo.Stack = FastStack<ComponentID>.Create(8);
            }
            //fill up the table with the correct IDs
            //works for initalization as well as updating it
            for (ref int i = ref appliesTo.NextComponentIndex; i < Component.ComponentTable.Count; i++)
            {
                var id = new ComponentID((ushort)i);
                if (GenerationServices.TypeAttributeCache.TryGetValue(attributeType, out var compSet) && compSet.Contains(id.Type))
                {
                    appliesTo.Stack.Push(id);
                }
            }

            foreach (var compid in appliesTo.Stack.AsSpan())
            {
                foreach (var item in _enabledArchetypes.AsSpan())
                {
                    item.Archetype(this).Update(this, compid);
                }
            }
        }
        finally
        {
            ExitDisallowState();
        }
    }

    /// <summary>
    /// Creates a custom query from the given set of rules. For an entity to be queried, all rules must apply.
    /// </summary>
    /// <param name="rules">The rules governing which entities are queried.</param>
    /// <returns>A query object representing all the entities that satisfy all the rules.</returns>
    public Query CustomQuery(params Rule[] rules)
    {
        QueryHash queryHash = QueryHash.New();
        foreach (Rule rule in rules)
            queryHash.AddRule(rule);

        return CollectionsMarshal.GetValueRefOrAddDefault(QueryCache, queryHash.ToHashCodeIncludeDisable(), out _) ??= CreateQueryFromSpan([.. rules]);
    }

    //we could use static abstract methods IF NOT FOR DOTNET6
    /// <summary>
    /// Gets a query specified by <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The created or cached query.</returns>
    public Query Query<T>()
        where T : struct, IConstantQueryHashProvider
    {
        ref Query? cachedValue = ref CollectionsMarshal.GetValueRefOrAddDefault(QueryCache, default(T).GetHashCode(), out bool exists);
        if (!exists)
        {
            cachedValue = CreateQuery(default(T).Rules);
        }
        return cachedValue!;
    }

    internal void ArchetypeAdded(ArchetypeID archetype)
    {
        if (!GlobalWorldTables.HasTag(archetype, Tag<Disable>.ID))
            _enabledArchetypes.Push(archetype);
        foreach (var qkvp in QueryCache)
        {
            qkvp.Value.TryAttachArchetype(archetype.Archetype(this));
        }
    }

    internal Query CreateQuery(ImmutableArray<Rule> rules)
    {
        Query q = new Query(this, rules);
        foreach (ref var element in WorldArchetypeTable.AsSpan())
            if (element is not null)
                q.TryAttachArchetype(element);
        return q;
    }

    internal Query CreateQueryFromSpan(ReadOnlySpan<Rule> rules) => CreateQuery(MemoryHelpers.ReadOnlySpanToImmutableArray(rules));

    internal void UpdateArchetypeTable(int newSize)
    {
        Debug.Assert(newSize > WorldArchetypeTable.Length);
        FastStackArrayPool<Archetype>.ResizeArrayFromPool(ref WorldArchetypeTable, newSize);
    }

    internal void EnterDisallowState()
    {
        Interlocked.Increment(ref _allowStructuralChanges);
    }

    internal void ExitDisallowState()
    {
        if (Interlocked.Decrement(ref _allowStructuralChanges) == 0)
        {
            //i plan on adding events later, so even more command buffer events could be added during playback
            while (WorldUpdateCommandBuffer.Playback()) ;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EventRecord TryGetEventData(EntityLocation entityLocation, EntityIDOnly entity, EntityFlags eventType, out bool exists)
    {
        if (entityLocation.HasEvent(eventType))
        {
            exists = true;
            return ref CollectionsMarshal.GetValueRefOrNullRef(EventLookup, entity);
        }


        exists = false;
        return ref Unsafe.NullRef<EventRecord>();
    }

    internal bool AllowStructualChanges => _allowStructuralChanges == 0;

    /// <summary>
    /// Disposes of the <see cref="World"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            throw new InvalidOperationException("World is already disposed!");

        GlobalWorldTables.Worlds[ID] = null!;

        if (WorldCachePackedValue == PackedIDVersion)
        {
            for (int i = 0; i < 10; i++)
            {
                WorldCachePackedValue = DefaultWorldCachePackedValue;
            }
            QuickWorldCache = null!;
        }

        _recycledWorldIDs.Push((ID, unchecked((byte)(Version - 1))));
        foreach (ref var item in WorldArchetypeTable.AsSpan())
            if (item is not null)
                item.ReleaseArrays();

        _sharedCountdown.Dispose();

        _isDisposed = true;

        _recycledEntityIds.Dispose();
        EntityTable.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="Entity"/>
    /// </summary>
    /// <param name="components">The components to use</param>
    /// <returns>The created entity</returns>
    /// <exception cref="ArgumentException">Thrown when the length of <paramref name="components"/> is > 16.</exception>
    public Entity CreateFromObjects(ReadOnlySpan<object> components)
    {
        if (components.Length < 0 || components.Length > 16)
            throw new ArgumentException("0-16 components per entity only", nameof(components));

        Span<ComponentID> types = stackalloc ComponentID[components.Length];

        for (int i = 0; i < components.Length; i++)
            types[i] = Component.GetComponentID(components[i].GetType());

        Archetype archetype = Archetype.CreateOrGetExistingArchetype(types!, [], this);

        ref EntityIDOnly entityID = ref archetype.CreateEntityLocation(EntityFlags.None, out EntityLocation loc);
        Entity entity = CreateEntityFromLocation(loc);
        entityID.ID = entity.EntityID;
        entityID.Version = entity.EntityVersion;

        Span<IComponentRunner> archetypeComponents = archetype.Components.AsSpan()[..components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            archetypeComponents[i].SetAt(components[i], loc.Index);
        }

        EntityCreatedEvent.Invoke(entity);
        return entity;
    }

    /// <summary>
    /// Creates an <see cref="Entity"/> with zero components.
    /// </summary>
    /// <returns>The entity that was created.</returns>
    public Entity Create()
    {
        var entity = CreateEntityWithoutEvent();
        EntityCreatedEvent.Invoke(entity);
        return entity;
    }

    internal Entity CreateEntityWithoutEvent()
    {
        var archetypeID = Archetype.Default;
        //TODO: replace this with static field
        Archetype archetype = Archetype.CreateOrGetExistingArchetype([], [], this, ImmutableArray<ComponentID>.Empty, ImmutableArray<TagID>.Empty);
        ref var entity = ref archetype.CreateEntityLocation(EntityFlags.None, out var eloc);

        var (id, version) = entity = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, 0);
        EntityTable[id] = new(eloc, version);

        return new Entity(ID, Version, version, id);
    }

    internal void InvokeEntityCreated(Entity entity)
    {
        EntityCreatedEvent.Invoke(entity);
    }

    /// <summary>
    /// Allocates memory sufficient to store <paramref name="componentTypes"/> entities of a type
    /// </summary>
    /// <param name="componentTypes">The types of the entity to allocate for</param>
    /// <param name="count">Number of entity spaces to allocate</param>
    /// <remarks>Use this method when creating a large number of entities</remarks>
    /// <exception cref="ArgumentException">Thrown when the length of <paramref name="componentTypes"/> is > 16.</exception>
    public void EnsureCapacity(ReadOnlySpan<ComponentID> componentTypes, int count)
    {
        if (componentTypes.Length == 0 || componentTypes.Length > 16)
            throw new ArgumentException("1-16 components per entity only", nameof(componentTypes));
        if(count < 1)
            throw new ArgumentOutOfRangeException("count must be positive", nameof(count));
        Archetype archetype = Archetype.CreateOrGetExistingArchetype(componentTypes, [], this);
        EnsureCapacityCore(archetype, count);
    }

    internal void EnsureCapacityCore(Archetype archetype, int count)
    {
        if(count < 1)
            throw new ArgumentOutOfRangeException("Count must be positive", nameof(count));
        archetype.EnsureCapacity(count);
        EntityTable.EnsureCapacity(count + EntityCount);
    }

    internal class NullUniformProvider : IUniformProvider
    {
        internal static NullUniformProvider Instance { get; } = new NullUniformProvider();

        [DebuggerHidden]
        public T GetUniform<T>()
        {
            FrentExceptions.Throw_InvalidOperationException("Initialize the world with an IUniformProvider in order to use uniforms");
            return default!;
        }
    }

    [DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
    internal record struct EntityLookup(EntityLocation Location, ushort Version)
    {
        internal EntityLocation Location = Location;
        internal ushort Version = Version;
        private readonly string DebuggerDisplayString => $"Archetype {Location.ArchetypeID}, Component: {Location.Index}, Version: {Version}";
    }
}