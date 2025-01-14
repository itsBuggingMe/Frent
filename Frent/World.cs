using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("Frent.Tests")]
namespace Frent;

/// <summary>
/// Represents a collection of entities that can be updated and queried
/// </summary>
public partial class World : IDisposable
{
    #region Static Version Management
    private static FastStack<(byte ID, byte Version)> _recycledWorldIDs = FastStack<(byte ID, byte Version)>.Create(16);
    private static byte _nextWorldID;
    private static int _numWorldWithStructChange = 0;
    internal static Action? ClearTempComponentStorage;
    #endregion


    internal Table<EntityLookup> EntityTable = new Table<EntityLookup>(32);
    private Archetype[] WorldArchetypeTable;
    private FastStack<(int ID, ushort Version)> _recycledEntityIds = FastStack<(int, ushort)>.Create(8);
    private Dictionary<Type, (FastStack<ComponentID> Stack, int NextComponentIndex)> _updatesByAttributes = [];
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;
    private bool _isDisposed = false;

    internal Dictionary<int, Query> QueryCache = [];

    private FastStack<Archetype> _enabledArchetypes = FastStack<Archetype>.Create(16);

    private volatile int _allowStructuralChanges;

    #region Operations
    internal FastStack<EntityIDOnly> DeleteEntityBuffer = FastStack<EntityIDOnly>.Create(4);
    internal FastStack<AddComponent> AddComponentBuffer = FastStack<AddComponent>.Create(4);
    internal FastStack<DeleteComponent> RemoveComponentBuffer = FastStack<DeleteComponent>.Create(4);
    #endregion


    /// <summary>
    /// The current uniform provider used when updating components/queries with uniforms
    /// </summary>
    public IUniformProvider UniformProvider { get; set; }

    /// <summary>
    /// Gets the current number of entities managed by the world
    /// </summary>
    public int EntityCount => _nextEntityID - _recycledEntityIds.Count;

    /// <summary>
    /// The current world config
    /// </summary>
    public Config CurrentConfig { get; set; }

    /// <summary>
    /// Creates a world with zero entities and a uniform provider
    /// </summary>
    /// <param name="uniformProvider">The initial uniform provider to be used</param>
    /// <param name="config">The inital config to use. If not provided <see cref="Config.Singlethreaded"/> is used.</param>
    public World(IUniformProvider? uniformProvider = null, Config? config = null)
    {
        CurrentConfig = config ?? Config.Singlethreaded;
        UniformProvider = uniformProvider ?? NullUniformProvider.Instance;
        (ID, Version) = _recycledWorldIDs.TryPop(out var id) ? id : (_nextWorldID++, byte.MaxValue);
        IDAsUInt = ID;
        GlobalWorldTables.Worlds[ID] = this;

        WorldArchetypeTable = new Archetype[GlobalWorldTables.ComponentTagLocationTable.Length];
    }

    internal Entity CreateEntityFromLocation(EntityLocation entityLocation)
    {
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : (_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = new(entityLocation, version);
        return new Entity(ID, Version, version, id);
    }

    /// <summary>
    /// Updates all component instances in the world that implement a component interface, e.g., <see cref="IUpdateComponent"/>
    /// </summary>
    public void Update()
    {
        EnterDisallowState();

        if (CurrentConfig.MultiThreadedUpdate)
        {
            foreach (var element in _enabledArchetypes.AsSpan())
            {
                element.MultiThreadedUpdate(CurrentConfig);
            }
        }
        else
        {
            foreach (var element in _enabledArchetypes.AsSpan())
            {
                element.Update();
            }
        }

        ExitDisallowState();
    }

    public void Update<T>() where T : Attribute => Update(typeof(T));
    
    public void Update(Type attributeType)
    {
        EnterDisallowState();

        ref var appliesTo = ref CollectionsMarshal.
            GetValueRefOrAddDefault(_updatesByAttributes, attributeType, out bool exists); 
        if(!exists)
        {
            appliesTo.Stack = FastStack<ComponentID>.Create(8);
        }
        //fill up the table with the correct IDs
        //works for initalization as well as updating it
        for(ref int i = ref appliesTo.NextComponentIndex; i < Component.ComponentTable.Count; i++)
        {
            if(ComponentHasAttributeOfType(new(i), attributeType))
            {
                appliesTo.Stack.Push(new(i));
            }
        }

        foreach(var compid in appliesTo.Stack.AsSpan())
        {
            foreach(var item in _enabledArchetypes.AsSpan())
            {
                item?.Update(compid);
            }
        }

        ExitDisallowState();
    }
    
    private static bool ComponentHasAttributeOfType(ComponentID id, Type t)
    {
        var member = id
            .Type
            .GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
        if(member is null)
            return false;
        return Attribute.IsDefined(member, t);
    }

    internal ref Archetype GetArchetype(uint archetypeID)
    {
        return ref WorldArchetypeTable[archetypeID];
    }

    internal void ArchetypeAdded(Archetype archetype)
    {
        if (!GlobalWorldTables.HasTag(archetype.ID.ID, Tag<Disable>.ID))
            _enabledArchetypes.Push(archetype);
        foreach (var qkvp in QueryCache)
        {
            qkvp.Value.TryAttachArchetype(archetype);
        }
    }

    internal Query CreateQuery(params Rule[] rules)
    {
        Query q = new Query(rules);
        foreach (var element in WorldArchetypeTable.AsSpan())
            if (element is not null)
                q.TryAttachArchetype(element);
        return q;
    }

    internal void UpdateArchetypeTable(int newSize)
    {
        Debug.Assert(newSize > WorldArchetypeTable.Length);
        FastStackArrayPool<Archetype>.ResizeArrayFromPool(ref WorldArchetypeTable, newSize);
    }

    internal void EnterDisallowState()
    {
        Interlocked.Increment(ref _numWorldWithStructChange);
        Interlocked.Increment(ref _allowStructuralChanges);
    }

    internal void ExitDisallowState()
    {
        if(Interlocked.Decrement(ref _allowStructuralChanges) == 0)
        {
            
            while (RemoveComponentBuffer.TryPop(out var item))
            {
                var id = (uint)item.Entity.ID;
                var record = EntityTable[id];
                if (record.Version == item.Entity.Version)
                {
                    RemoveComponent(item.Entity.ToEntity(this), record.Location, item.ComponentID);
                }
            }

            while (AddComponentBuffer.TryPop(out var command))
            {
                var id = (uint)command.Entity.ID;
                var record = EntityTable[id];
                if (record.Version == command.Entity.Version)
                {
                    AddComponent(command.Entity.ToEntity(this), record.Location, command.ComponentID, 
                        out var runner, 
                        out var location);
                    runner.PullComponentFrom(Component.ComponentTable[command.ComponentID.ID].Stack, location, command.Index);
                }
            }

            while (DeleteEntityBuffer.TryPop(out var item))
            {
                //double check that its alive
                var record = EntityTable[(uint)item.ID];
                if (record.Version == item.Version)
                {
                    DeleteEntity(item.ID, item.Version, record.Location);
                }
            }

            if (Interlocked.Decrement(ref _numWorldWithStructChange) == 0)
            {
                ClearTempComponentStorage?.Invoke();
            }
        }
    }

    internal bool AllowStructualChanges => _allowStructuralChanges == 0;

    /// <summary>
    /// Disposes of the <see cref="World"/>
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            throw new InvalidOperationException("World is already disposed!");

        GlobalWorldTables.Worlds[ID] = null!;
        _recycledWorldIDs.Push((ID, unchecked((byte)(Version - 1))));
        foreach(var item in WorldArchetypeTable.AsSpan())
            if(item is not null)
                item.ReleaseArrays();


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

        //poverty InlineArray
        Span<Type?> types = ((Span<Type?>)([null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null]))[..components.Length];

        for (int i = 0; i < components.Length; i++)
            types[i] = components[i].GetType();

        Archetype archetype = Archetype.CreateOrGetExistingArchetype(types!, [], this);
        ref Entity entity = ref archetype.CreateEntityLocation(out EntityLocation loc);
        entity = CreateEntityFromLocation(loc);

        Span<IComponentRunner> archetypeComponents = archetype.Components.AsSpan()[..components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            archetypeComponents[i].SetAt(components[i], loc.ChunkIndex, loc.ComponentIndex);
        }

        return entity;
    }

    /// <summary>
    /// Allocates memory sufficient to store <paramref name="componentTypes"/> entities of a type
    /// </summary>
    /// <param name="componentTypes">The types of the entity to allocate for</param>
    /// <param name="count">Number of entity spaces to allocate</param>
    /// <remarks>Use this method when creating a large number of entities</remarks>
    /// <exception cref="ArgumentException">Thrown when the length of <paramref name="componentTypes"/> is > 16.</exception>
    public void EnsureCapacity(ReadOnlySpan<Type> componentTypes, int count)
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
        private string DebuggerDisplayString => $"Archetype {Location.ArchetypeID}, Chunk: {Location.ChunkIndex}, Component: {Location.ComponentIndex}, Version: {Version}";
    }
}