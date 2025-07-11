﻿using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Systems;
using Frent.Systems.Queries;
using Frent.Updating;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

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

    //entityID -> entity metadata
    internal Table<EntityLocation> EntityTable = new Table<EntityLocation>(256);
    //archetype ID -> Archetype?
    internal WorldArchetypeTableItem[] WorldArchetypeTable;
    
    internal struct WorldArchetypeTableItem(Archetype archetype, Archetype temp)
    {
        public Archetype Archetype = archetype;
        public Archetype DeferredCreationArchetype = temp;
    }

    internal Dictionary<ArchetypeEdgeKey, Archetype> ArchetypeGraphEdges = [];

    internal NativeStack<EntityIDOnly> RecycledEntityIds = new NativeStack<EntityIDOnly>(256);
    
    private Dictionary<Type, WorldUpdateFilter> _updatesByAttributes = [];
    private Dictionary<ComponentID, SingleComponentUpdateFilter> _singleComponentUpdates = [];
    internal int NextEntityID;

    internal readonly ushort ID;
    internal readonly Entity DefaultWorldEntity;
    private bool _isDisposed = false;

    internal volatile bool _worldUpdateMethodCalled;

    internal void EnterWorldUpdateMethod()
    {
        if(_worldUpdateMethodCalled)
            FrentExceptions.Throw_InvalidOperationException("Nested World.Update calls are not supported!");
        _worldUpdateMethodCalled = true;
    }

    internal void ExitWorldUpdateMethod()
    {
        _worldUpdateMethodCalled = false;
    }

    internal FastStack<Query> QueryCache = new FastStack<Query>(4);

    internal CountdownEvent SharedCountdown => _sharedCountdown;
    private CountdownEvent _sharedCountdown = new(0);
    internal FastStack<ArchetypeID> EnabledArchetypes = FastStack<ArchetypeID>.Create(16);

    // -1: normal state
    // 0: some kind of transition in End/Enter
    // n: n systems/updates active
    private int _allowStructuralChanges = -1;

    internal CommandBuffer WorldUpdateCommandBuffer;

    internal EntityOnlyEvent EntityCreatedEvent = new EntityOnlyEvent();
    internal EntityOnlyEvent EntityDeletedEvent = new EntityOnlyEvent();
    internal Event<ComponentID> ComponentAddedEvent = new Event<ComponentID>();
    internal Event<ComponentID> ComponentRemovedEvent = new Event<ComponentID>();
    internal TagEvent Tagged = new TagEvent();
    internal TagEvent Detached = new TagEvent();

    //these lookups exists for programmical api optimization
    //normal <T1, T2...> methods use a shared global static cache
    internal FastLookup AddComponentLookup = new();
    internal FastLookup RemoveComponentLookup = new();
    internal FastLookup AddTagLookup = new();
    internal FastLookup RemoveTagLookup = new();


    internal EntityFlags WorldEventFlags;

    internal FastStack<ArchetypeDeferredUpdateRecord> DeferredCreationArchetypes = FastStack<ArchetypeDeferredUpdateRecord>.Create(4);
    private FastStack<ArchetypeDeferredUpdateRecord> _altDeferredCreationArchetypes = FastStack<ArchetypeDeferredUpdateRecord>.Create(4);

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

    internal Dictionary<EntityIDOnly, EventRecord> EventLookup = [];
    internal readonly Archetype DefaultArchetype;

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

        WorldArchetypeTable = new WorldArchetypeTableItem[GlobalWorldTables.ComponentTagLocationTable.Length];

        WorldUpdateCommandBuffer = new CommandBuffer(this);
        DefaultWorldEntity = new Entity(ID, default, default);
        DefaultArchetype = Archetype.CreateOrGetExistingArchetype([], [], this, ImmutableArray<ComponentID>.Empty, ImmutableArray<TagID>.Empty);
    }

    internal Entity CreateEntityFromLocation(EntityLocation entityLocation)
    {
        var (id, version) = RecycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(NextEntityID++, (ushort)0);
        entityLocation.Version = version;
        EntityTable[id] = entityLocation;
        return new Entity(ID, version, id);
    }

    /// <summary>
    /// Updates all component instances in the world that implement a component interface, e.g., <see cref="IComponent"/>
    /// </summary>
    public void Update()
    {
        EnterDisallowState();
        EnterWorldUpdateMethod();
        try
        {
            if (CurrentConfig.MultiThreadedUpdate)
            {
                throw new NotSupportedException();
            }
            else
            {
                foreach (var element in EnabledArchetypes.AsSpan())
                {
                    element.Archetype(this)!.Update(this);
                }
            }
        }
        finally
        {
            ExitWorldUpdateMethod();
            ExitDisallowState(null, CurrentConfig.UpdateDeferredCreationEntities);
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
        EnterWorldUpdateMethod();
        WorldUpdateFilter? appliesTo = default;
        try
        {
            if (!_updatesByAttributes.TryGetValue(attributeType, out appliesTo))
                _updatesByAttributes[attributeType] = appliesTo = new WorldUpdateFilter(this, attributeType);
            appliesTo.Update();
        }
        finally
        {
            ExitWorldUpdateMethod();
            ExitDisallowState(appliesTo, CurrentConfig.UpdateDeferredCreationEntities);
        }
    }

    /// <summary>
    /// Updates all instances of a specific component type.
    /// </summary>
    /// <param name="componentType"></param>
    public void UpdateComponent(ComponentID componentType)
    {
        EnterDisallowState();
        EnterWorldUpdateMethod();
        SingleComponentUpdateFilter? singleComponent = null;

        try
        {
#if NETSTANDARD2_1
        if(!_singleComponentUpdates.TryGetValue(componentType, out singleComponent))
            _singleComponentUpdates[componentType] = singleComponent = new(this, componentType);
#else
            singleComponent = CollectionsMarshal.GetValueRefOrAddDefault(_singleComponentUpdates, componentType, out _) ??= new(this, componentType);
#endif

            singleComponent.Update();  
        }
        finally
        {
            ExitWorldUpdateMethod();
            ExitDisallowState(singleComponent, CurrentConfig.UpdateDeferredCreationEntities);
        }
    }

    internal void ArchetypeAdded(Archetype archetype, Archetype temporaryCreationArchetype)
    {
        if (!GlobalWorldTables.HasTag(archetype.ID, Tag<Disable>.ID))
            EnabledArchetypes.Push(archetype.ID);
        foreach (var qkvp in QueryCache)
            qkvp.TryAttachArchetype(archetype);
        foreach (var fkvp in _updatesByAttributes)
            fkvp.Value.ArchetypeAdded(archetype);
        foreach(var fkvp in _singleComponentUpdates)
            fkvp.Value.ArchetypeAdded(archetype);
    }

    internal Query CreateQuery(ImmutableArray<Rule> rules)
    {
        Query q = new Query(this, rules);
        QueryCache.Push(q);
        foreach (ref var element in WorldArchetypeTable.AsSpan())
            if (element.Archetype is not null)
                q.TryAttachArchetype(element.Archetype);
        return q;
    }


    /// <summary>
    /// Returns a query builder that can be used to create a query with the specified rules.
    /// </summary>
    public QueryBuilder CreateQuery() => new QueryBuilder(this);


    internal Query CreateQueryFromSpan(ReadOnlySpan<Rule> rules) => CreateQuery(MemoryHelpers.ReadOnlySpanToImmutableArray(rules));

    internal Query BuildQuery<T>()
        where T : struct, IQueryBuilder
    {
        ref Query query = ref QueryInfo<T>.Queries[ID];
        query ??= QueryInfo<T>.Build(this);
        return query;
    }

    internal static class QueryInfo<T>
        where T : struct, IQueryBuilder
    {
        public static readonly ShortSparseSet<Query> Queries = new();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Query Build(World world)
        {
            List<Rule> rules = [];
            default(T).AddRules(rules);
            return world.CreateQuery(rules.ToImmutableArray());
        }
    }

    internal void UpdateArchetypeTable(int newSize)
    {
        Debug.Assert(newSize > WorldArchetypeTable.Length);
        Array.Resize(ref WorldArchetypeTable, newSize);

        World world = GlobalWorldTables.Worlds[ID];
    }

    internal void EnterDisallowState()
    {
        if(Interlocked.Increment(ref _allowStructuralChanges) == 0)
        {
            Interlocked.Increment(ref _allowStructuralChanges);
        }
    }
    
    const int DeferredEntityOperationRecursionLimit = 200;

    internal void ExitDisallowState(IComponentUpdateFilter? filterUsed, bool updateDeferredEntities = false)
    {
        if (Interlocked.Decrement(ref _allowStructuralChanges) == 0)
        {
            if(DeferredCreationArchetypes.Count > 0)
            {
                if (updateDeferredEntities)
                {
                    ResolveUpdateDeferredCreationEntities(filterUsed);
                }
                else
                {
                    foreach (var (archetype, tmp, _) in DeferredCreationArchetypes.AsSpan())
                        archetype.ResolveDeferredEntityCreations(this, tmp);
                }
            }

            DeferredCreationArchetypes.ClearWithoutClearingGCReferences();
            Interlocked.Decrement(ref _allowStructuralChanges);

            int count = 0;
            while (WorldUpdateCommandBuffer.Playback())
                if(++count > DeferredEntityOperationRecursionLimit)
                    FrentExceptions.Throw_InvalidOperationException("Deferred entity creation recursion limit exceeded! Are your component events creating command buffer items? (which create more command buffer items...)?");
        }
    }

    private void ResolveUpdateDeferredCreationEntities(IComponentUpdateFilter? filterUsed)
    {
        Span<ArchetypeDeferredUpdateRecord> resolveArchetypes = DeferredCreationArchetypes.AsSpan();

        Interlocked.Increment(ref _allowStructuralChanges);

        int createRecursionCount = 0;
        while (resolveArchetypes.Length != 0)
        {
            foreach (var (archetype, tmp, _) in resolveArchetypes)
                archetype.ResolveDeferredEntityCreations(this, tmp);

            (_altDeferredCreationArchetypes, DeferredCreationArchetypes) = (DeferredCreationArchetypes, _altDeferredCreationArchetypes);
            DeferredCreationArchetypes.ClearWithoutClearingGCReferences();

            if (filterUsed is not null)
            {
                filterUsed?.UpdateSubset(resolveArchetypes);
            }
            else
            {
                foreach (var (archetype, _, start) in resolveArchetypes)
                {
                    archetype.Update(this, start, archetype.EntityCount - start);
                }
            }

            resolveArchetypes = DeferredCreationArchetypes.AsSpan();

            if (++createRecursionCount > DeferredEntityOperationRecursionLimit)
            {
                FrentExceptions.Throw_InvalidOperationException("Deferred entity creation recursion limit exceeded! Are your components creating entities (which create more entities...)?");
            }
        }

        DeferredCreationArchetypes.ClearWithoutClearingGCReferences();
        Interlocked.Decrement(ref _allowStructuralChanges);
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

    internal bool AllowStructualChanges => _allowStructuralChanges == -1;

    /// <summary>
    /// Disposes of the <see cref="World"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            throw new InvalidOperationException("World is already disposed!");

        GlobalWorldTables.Worlds[ID] = null!;

        foreach (ref var item in WorldArchetypeTable.AsSpan())
        {
            if(item.Archetype is not null)
            {
                item.Archetype.ReleaseArrays(false);
                item.DeferredCreationArchetype.ReleaseArrays(true);
            }
        }

        _sharedCountdown.Dispose();

        _isDisposed = true;

        RecycledEntityIds.Dispose();
        //EntityTable.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="Entity"/>.
    /// </summary>
    /// <param name="components">The components to use.</param>
    /// <returns>The created entity.</returns>
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
        entityID.Init(entity);

        Span<ComponentStorageRecord> archetypeComponents = archetype.Components.AsSpan();
        for (int i = 1; i < archetypeComponents.Length; i++)
        {
            archetypeComponents[i].SetAt(null, components[i - 1], loc.Index);
        }
        for (int i = 1; i < archetypeComponents.Length; i++)
        {
            archetypeComponents[i].CallIniter(entity, loc.Index);
        }

        EntityCreatedEvent.Invoke(entity);
        return entity;
    }

    /// <summary>
    /// Creates an <see cref="Entity"/> from a set of component handles. The handles are not disposed.
    /// </summary>
    /// <returns>The created entity.</returns>
    public Entity CreateFromHandles(ReadOnlySpan<ComponentHandle> componentHandles, ReadOnlySpan<TagID> tags = default)
    {
        if (componentHandles.Length > MemoryHelpers.MaxComponentCount)
            throw new ArgumentException("Max 127 components on an entity", nameof(componentHandles));

        Span<ComponentID> componentIDs = stackalloc ComponentID[componentHandles.Length];

        for (int i = 0; i < componentHandles.Length; i++)
            componentIDs[i] = componentHandles[i].ComponentID;

        Archetype archetype = Archetype.CreateOrGetExistingArchetype(componentIDs, tags, this);

        ref EntityIDOnly entityID = ref archetype.CreateEntityLocation(EntityFlags.None, out EntityLocation entityLocation);

        Entity entity = CreateEntityFromLocation(entityLocation);
        entityID.Init(entity);

        Span<ComponentStorageRecord> archetypeComponents = archetype.Components.AsSpan();
        for (int i = 1; i < archetypeComponents.Length; i++)
        {
            archetypeComponents[i].SetAt(null, componentHandles[i - 1], entityLocation.Index);
        }
        for (int i = 1; i < archetypeComponents.Length; i++)
        {
            archetypeComponents[i].CallIniter(entity, entityLocation.Index);
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
        ref var entity = ref DefaultArchetype.CreateEntityLocation(EntityFlags.None, out var eloc);
        Entity result = CreateEntityFromLocation(eloc);
        entity.Init(result);
        return result;
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