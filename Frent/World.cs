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

    internal static Table<EntityLookup> QuickWorkTable = new Table<EntityLookup>(32);

    internal static readonly ushort DefaultWorldCachePackedValue = new Entity(byte.MaxValue, byte.MaxValue, 0, 0).PackedWorldInfo;

    //the idea is what work is usually done at one world at a time
    //we can save a lookup and a deref by storing it statically
    //this saves a few nano seconds and makes the public entity apis 2x as fast
    internal static volatile World? QuickWorldCache;
    internal static volatile ushort WorldCachePackedValue = DefaultWorldCachePackedValue;

    internal Table<EntityLookup> EntityTable = new Table<EntityLookup>(32);
    internal Archetype[] WorldArchetypeTable;
    internal Dictionary<ArchetypeEdgeKey, ArchetypeID> ArchetypeGraphEdges = [];

    private FastStack<EntityIDOnly> _recycledEntityIds = FastStack<EntityIDOnly>.Create(8);
    private Dictionary<Type, (FastStack<ComponentID> Stack, int NextComponentIndex)> _updatesByAttributes = [];
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;
    internal readonly ushort PackedIDVersion;
    private bool _isDisposed = false;

    internal Dictionary<int, Query> QueryCache = [];

    internal CountdownEvent SharedCountdown => _sharedCountdown;
    private CountdownEvent _sharedCountdown = new(0);
    private FastStack<ArchetypeID> _enabledArchetypes = FastStack<ArchetypeID>.Create(16);

    private volatile int _allowStructuralChanges;

    internal CommandBuffer WorldUpdateCommandBuffer;

    internal EntityOnlyEvent _entityCreated = new EntityOnlyEvent();
    internal EntityOnlyEvent _entityDeleted = new EntityOnlyEvent();
    internal Event<ComponentID> _componentAdded = new Event<ComponentID>();
    internal Event<ComponentID> _componentRemoved = new Event<ComponentID>();
    internal Event<TagID> _tagged = new Event<TagID>();
    internal Event<TagID> _detached = new Event<TagID>();
    internal EntityFlags _worldEventFlags; 

    /// <summary>
    /// Invoked whenever an entity is created on this world.
    /// </summary>
    public event Action<Entity>? EntityCreated 
    { 
        add
        {
            _entityCreated.Add(value);
            _worldEventFlags |= EntityFlags.WorldCreate;
        } 
        remove
        {
            _entityCreated.Remove(value);
            if(!_entityCreated.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldCreate;
        }
    }
    /// <summary>
    /// Invoked whenever an entity belonging to this world is deleted.
    /// </summary>
    public event Action<Entity>? EntityDeleted
    { 
        add
        {
            _entityDeleted.Add(value);
            _worldEventFlags |= EntityFlags.WorldOnDelete;
        } 
        remove
        {
            _entityDeleted.Remove(value);
            if(!_entityDeleted.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldOnDelete;
        }
    }

    /// <summary>
    /// Invoked whenever a component is added to an entity.
    /// </summary>
    public event Action<Entity, ComponentID>? ComponentAdded
    {
        add
        {
            _componentAdded.Add(value);
            _worldEventFlags |= EntityFlags.WorldAddComp;
        } 
        remove
        {
            _componentAdded.Remove(value);
            if(!_componentAdded.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldAddComp;
        }
    }

    /// <summary>
    /// Invoked whenever a component is removed from an entity.
    /// </summary>
    public event Action<Entity, ComponentID>? ComponentRemoved
    {
        add
        {
            _componentRemoved.Add(value);
            _worldEventFlags |= EntityFlags.WorldRemoveComp;
        } 
        remove
        {
            _componentRemoved.Remove(value);
            if(!_componentRemoved.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldRemoveComp;
        }
    }

    /// <summary>
    /// Invoked whenever a tag is added to an entity.
    /// </summary>
    public event Action<Entity, TagID>? TagTagged
    {
        add
        {
            _tagged.Add(value);
            _worldEventFlags |= EntityFlags.WorldTagged;
        } 
        remove
        {
            _tagged.Remove(value);
            if(!_tagged.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldTagged;
        }
    }

    /// <summary>
    /// Invoked whenever a tag is removed from an entity.
    /// </summary>
    public event Action<Entity, TagID>? TagDetached
    {
        add
        {
            _detached.Add(value);
            _worldEventFlags |= EntityFlags.WorldDetach;
        } 
        remove
        {
            _detached.Remove(value);
            if(!_detached.HasListeners)
                _worldEventFlags &= ~EntityFlags.WorldDetach;
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
    }

    internal Entity CreateEntityFromLocation(EntityLocation entityLocation)
    {
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = new(entityLocation, version);
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

        ref Entity entity = ref archetype.CreateEntityLocation(EntityFlags.None, out EntityLocation loc);
        entity = CreateEntityFromLocation(loc);

        Span<IComponentRunner> archetypeComponents = archetype.Components.AsSpan()[..components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            archetypeComponents[i].SetAt(components[i], loc.ChunkIndex, loc.ComponentIndex);
        }

        EntityCreated?.Invoke(entity);
        return entity;
    }

    /// <summary>
    /// Creates an <see cref="Entity"/> with zero components.
    /// </summary>
    /// <returns>The entity that was created.</returns>
    public Entity Create()
    {
        var entity = CreateEntityWithoutEvent();
        EntityCreated?.Invoke(entity);
        return entity;
    }

    internal Entity CreateEntityWithoutEvent()
    {
        var archetypeID = Archetype.Default;
        //TODO: replace this with static field
        Archetype archetype = Archetype.CreateOrGetExistingArchetype([], [], this, ImmutableArray<ComponentID>.Empty, ImmutableArray<TagID>.Empty);
        ref var entity = ref archetype.CreateEntityLocation(EntityFlags.None, out var eloc);

        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = new(eloc, version);

        entity = new Entity(ID, Version, version, id);
        return entity;
    }

    internal void InvokeEntityCreated(Entity entity)
    {
        EntityCreated?.Invoke(entity);
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
        Archetype archetype = Archetype.CreateOrGetExistingArchetype(componentTypes, [], this);
        EnsureCapacityCore(archetype, count);
    }

    internal void EnsureCapacityCore(Archetype archetype, int count)
    {
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
        private readonly string DebuggerDisplayString => $"Archetype {Location.ArchetypeID}, Chunk: {Location.ChunkIndex}, Component: {Location.ComponentIndex}, Version: {Version}";
    }
}