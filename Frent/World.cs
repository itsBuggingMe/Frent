using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

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
    #endregion


    internal Table<(EntityLocation Location, ushort Version)> EntityTable = new Table<(EntityLocation, ushort Version)>(32);
    private Archetype[] WorldArchetypeTable;
    private FastStack<(int ID, ushort Version)> _recycledEntityIds = FastStack<(int, ushort)>.Create(8);
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;
    private bool _isDisposed = false;

    internal Dictionary<int, Query> QueryCache = [];

    private FastStack<Archetype> _enabledArchetypes = FastStack<Archetype>.Create(16);

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
        EntityTable[(uint)id] = (entityLocation, version);
        return new Entity(ID, Version, version, id);
    }

    internal void DeleteEntityInternal(Entity entity, ref readonly EntityLocation entityLocation)
    {
        //entity is guaranteed to be alive here
        Entity replacedEntity = entityLocation.Archetype(this).DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        EntityTable[(uint)replacedEntity.EntityID] = (entityLocation, replacedEntity.EntityVersion);
        EntityTable[(uint)entity.EntityID] = (EntityLocation.Default, ushort.MaxValue);
    }

    /// <summary>
    /// Updates all component instances in the world that implement a component interface, e.g., <see cref="IUpdateComponent"/>
    /// </summary>
    public void Update()
    {
        if(CurrentConfig.MultiThreadedUpdate)
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
        MemoryHelpers<Archetype>.ResizeArrayFromPool(ref WorldArchetypeTable, newSize);
    }

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
        public T GetUniform<T>() => FrentExceptions.Throw_InvalidOperationException<T>("Initialize the world with an IUniformProvider in order to use uniforms");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Side() { }
}
