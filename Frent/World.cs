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
    private static ushort _nextWorldID = 1;
    #endregion

    internal Table<EntityLookup> EntityTable = new Table<EntityLookup>(256);
    internal Archetype[] WorldArchetypeTable;
    internal Dictionary<ArchetypeEdgeKey, Archetype> ArchetypeGraphEdges = [];

    internal NativeStack<EntityIDOnly> RecycledEntityIds = new NativeStack<EntityIDOnly>(256);
    private Dictionary<Type, WorldUpdateFilter> _updatesByAttributes = [];
    internal int NextEntityID;

    internal readonly ushort ID;
    internal readonly Entity DefaultWorldEntity;
    private bool _isDisposed = false;

    internal Dictionary<int, Query> QueryCache = [];

    internal CountdownEvent SharedCountdown => _sharedCountdown;
    private CountdownEvent _sharedCountdown = new(0);
    private FastStack<ArchetypeID> _enabledArchetypes = FastStack<ArchetypeID>.Create(16);

    private int _allowStructuralChanges;

    internal CommandBuffer WorldUpdateCommandBuffer;

    internal EntityOnlyEvent EntityCreatedEvent = new EntityOnlyEvent();
    internal EntityOnlyEvent EntityDeletedEvent = new EntityOnlyEvent();
    internal Event<ComponentID> ComponentAddedEvent = new Event<ComponentID>();
    internal Event<ComponentID> ComponentRemovedEvent = new Event<ComponentID>();
    internal TagEvent Tagged = new TagEvent();
    internal TagEvent Detached = new TagEvent();

    //these lookups exists for programmical api optimization
    //normal <T1, T2...> methods use a shared global static cache
    internal FastLookup AddComponentLookup;
    internal FastLookup RemoveComponentLookup;
    internal FastLookup AddTagLookup;
    internal FastLookup RemoveTagLookup;


    internal EntityFlags WorldEventFlags;

    internal FastStack<Archetype> DeferredCreationArchetypes = FastStack<Archetype>.Create(4);

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
            if (!EntityCreatedEvent.HasListeners)
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
            WorldEventFlags |= EntityFlags.OnDelete;
        }
        remove
        {
            EntityDeletedEvent.Remove(value);
            if (!EntityDeletedEvent.HasListeners)
                WorldEventFlags &= ~EntityFlags.OnDelete;
        }
    }

    /// <summary>
    /// Invoked whenever a component is added to an entity.
    /// </summary>
    public event Action<Entity, ComponentID> ComponentAdded
    {
        add => AddEvent(ref ComponentAddedEvent, value, EntityFlags.AddComp);
        remove => RemoveEvent(ref ComponentAddedEvent, value, EntityFlags.AddComp);
    }

    /// <summary>
    /// Invoked whenever a component is removed from an entity.
    /// </summary>
    public event Action<Entity, ComponentID> ComponentRemoved
    {
        add => AddEvent(ref ComponentRemovedEvent, value, EntityFlags.RemoveComp);
        remove => RemoveEvent(ref ComponentRemovedEvent, value, EntityFlags.RemoveComp);
    }

    /// <summary>
    /// Invoked whenever a tag is added to an entity.
    /// </summary>
    public event Action<Entity, TagID> TagTagged
    {
        add => AddEvent(ref Tagged, value, EntityFlags.Tagged);
        remove => RemoveEvent(ref Tagged, value, EntityFlags.Tagged);
    }

    /// <summary>
    /// Invoked whenever a tag is removed from an entity.
    /// </summary>
    public event Action<Entity, TagID> TagDetached
    {
        add => AddEvent(ref Detached, value, EntityFlags.Detach);
        remove => RemoveEvent(ref Detached, value, EntityFlags.Detach);
    }

    private void AddEvent<T>(ref Event<T> @event, Action<Entity, T> action, EntityFlags flag)
    {
        @event.Add(action);
        WorldEventFlags |= flag;
    }

    private void RemoveEvent<T>(ref Event<T> @event, Action<Entity, T> action, EntityFlags flag)
    {
        @event.Remove(action);
        if (!@event.HasListeners)
            WorldEventFlags &= ~flag;
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
    public int EntityCount => NextEntityID - RecycledEntityIds.Count;

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
        ID = _nextWorldID++;

        GlobalWorldTables.Worlds[ID] = this;

        WorldArchetypeTable = new Archetype[GlobalWorldTables.ComponentTagLocationTable.Length];

        WorldUpdateCommandBuffer = new CommandBuffer(this);
        DefaultWorldEntity = new Entity(ID, default, default);
        DefaultArchetype = Archetype.CreateOrGetExistingArchetype([], [], this, ImmutableArray<ComponentID>.Empty, ImmutableArray<TagID>.Empty);
        DeferredCreateArchetype = Archetype.CreateOrGetExistingArchetype(Archetype.DeferredCreate, this);
    }

    internal Entity CreateEntityFromLocation(EntityLocation entityLocation)
    {
        var (id, version) = RecycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(NextEntityID++, (ushort)0);
        EntityTable[id] = new(entityLocation, version);
        return new Entity(ID, version, id);
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
            if (!_updatesByAttributes.TryGetValue(attributeType, out WorldUpdateFilter? appliesTo))
                _updatesByAttributes[attributeType] = appliesTo = new WorldUpdateFilter();

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

        int hashCode = queryHash.ToHashCode();

        if (!QueryCache.TryGetValue(hashCode, out Query? query))
            QueryCache[hashCode] = query = CreateQueryFromSpan([.. rules]);

        return query;
    }

    internal void ArchetypeAdded(Archetype archetype)
    {
        if (!GlobalWorldTables.HasTag(archetype.ID, Tag<Disable>.ID))
            _enabledArchetypes.Push(archetype.ID);
        foreach (var qkvp in QueryCache)
        {
            qkvp.Value.TryAttachArchetype(archetype);
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
            Span<Archetype> resolveArchetypes = DeferredCreationArchetypes.AsSpan();

            foreach (var archetype in resolveArchetypes)
                archetype.ResolveDeferredEntityCreations(this);
            DeferredCreationArchetypes.ClearWithoutClearingGCReferences();

            //i plan on adding events later, so even more command buffer events could be added during playback
            while (WorldUpdateCommandBuffer.Playback()) ;
        }
    }

#if !NETSTANDARD2_1
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
#endif

    internal bool AllowStructualChanges => _allowStructuralChanges == 0;

    /// <summary>
    /// Disposes of the <see cref="World"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            throw new InvalidOperationException("World is already disposed!");

        GlobalWorldTables.Worlds[ID] = null!;

        foreach (ref var item in WorldArchetypeTable.AsSpan())
            if (item is not null)
                item.ReleaseArrays();

        _sharedCountdown.Dispose();

        _isDisposed = true;

        RecycledEntityIds.Dispose();
        //EntityTable.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="Entity"/>
    /// </summary>
    /// <param name="components">The components to use</param>
    /// <returns>The created entity</returns>
    public Entity CreateFromObjects(ReadOnlySpan<object> components)
    {
        if (components.Length > MemoryHelpers.MaxComponentCount)
            throw new ArgumentException("Max 127 components on an entity", nameof(components));
        Span<ComponentID> types = stackalloc ComponentID[components.Length];

        for (int i = 0; i < components.Length; i++)
            types[i] = Component.GetComponentID(components[i].GetType());

        Archetype archetype = Archetype.CreateOrGetExistingArchetype(types!, [], this);

        ref EntityIDOnly entityID = ref archetype.CreateEntityLocation(EntityFlags.None, out EntityLocation loc);
        Entity entity = CreateEntityFromLocation(loc);
        entityID.ID = entity.EntityID;
        entityID.Version = entity.EntityVersion;

        Span<ComponentStorageBase> archetypeComponents = archetype.Components.AsSpan()[..components.Length];
        for (int i = 1; i < archetypeComponents.Length; i++)
        {
            archetypeComponents[i].SetAt(components[i - 1], loc.Index);
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

    internal readonly Archetype DefaultArchetype;
    internal readonly Archetype DeferredCreateArchetype;
    internal Entity CreateEntityWithoutEvent()
    {
        ref var entity = ref DefaultArchetype.CreateEntityLocation(EntityFlags.None, out var eloc);

        var (id, version) = entity = RecycledEntityIds.CanPop() ? RecycledEntityIds.PopUnsafe() : new EntityIDOnly(NextEntityID++, 0);
        EntityTable[id] = new(eloc, version);

        return new Entity(ID, version, id);
    }

    internal void InvokeEntityCreated(Entity entity)
    {
        EntityCreatedEvent.Invoke(entity);
    }

    /// <summary>
    /// Allocates memory sufficient to store <paramref name="count"/> entities of a type
    /// </summary>
    /// <param name="entityType">The types of the entity to allocate for</param>
    /// <param name="count">Number of entity spaces to allocate</param>
    /// <remarks>Use this method when creating a large number of entities</remarks>
    public void EnsureCapacity(ArchetypeID entityType, int count)
    {
        if (count < 1)
            return;
        Archetype archetype = Archetype.CreateOrGetExistingArchetype(entityType, this);
        EnsureCapacityCore(archetype, count);
    }

    internal void EnsureCapacityCore(Archetype archetype, int count)
    {
        if (count < 1)
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
}