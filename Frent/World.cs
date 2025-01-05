using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Updating;
using System.Diagnostics;
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
    private Table<Archetype> WorldArchetypeTable = new Table<Archetype>(4);
    private FastStack<(int ID, ushort Version)> _recycledEntityIds = FastStack<(int, ushort)>.Create(8);
    private int _nextEntityID;

    internal readonly uint IDAsUInt;
    internal readonly byte ID;
    internal readonly byte Version;

    internal Dictionary<int, Query> QueryCache = [];

    /// <summary>
    /// The current uniform provider used when updating components/queries with uniforms
    /// </summary>
    public IUniformProvider UniformProvider { get; set; }

    /// <summary>
    /// Gets the current number of entities managed by the world
    /// </summary>
    public int EntityCount => _nextEntityID - _recycledEntityIds.Count;

    /// <summary>
    /// Creates a world with zero entities and a uniform provider
    /// </summary>
    /// <param name="uniformProvider">The initial uniform provider to be used</param>
    public World(IUniformProvider? uniformProvider = null)
    {
        UniformProvider = uniformProvider ?? NullUniformProvider.Instance;
        (ID, Version) = _recycledWorldIDs.TryPop(out var id) ? id : (_nextWorldID++, byte.MaxValue);
        IDAsUInt = ID;
        GlobalWorldTables.Worlds[ID] = this;
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
        foreach (var element in WorldArchetypeTable.AsSpan())
        {
            element?.Update();
        }
    }

    internal ref Archetype GetArchetype(uint archetypeID)
    {
        return ref WorldArchetypeTable[archetypeID];
    }

    internal void ArchetypeAdded(Archetype archetype)
    {
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

    /// <summary>
    /// Disposes of the <see cref="World"/>
    /// </summary>
    public void Dispose()
    {
        GlobalWorldTables.Worlds[ID] = null!;
        _recycledWorldIDs.Push((ID, unchecked((byte)(Version - 1))));
    }

    public Entity CreateFromObjects(ReadOnlySpan<object> components)
    {
        if (components.Length == 0 || components.Length > 16)
            throw new ArgumentException("1-16 components per entity only", nameof(components));
        Span<Type?> types = ((Span<Type?>)([null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null]))[..components.Length];

        for (int i = 0; i < components.Length; i++)
            types[i] = components[i].GetType();
        Archetype archetype = Archetype.CreateOrGetExistingArchetype(types!, this);
        ref Entity entity = ref archetype.CreateEntityLocation(out EntityLocation loc);
        entity = CreateEntityFromLocation(loc);

        Span<IComponentRunner> archetypeComponents = archetype.Components.AsSpan()[..components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            archetypeComponents[i].SetAt(components[i], loc.ChunkIndex, loc.ComponentIndex);
        }

        return entity;
    }

    public void EnsureCapacity(ReadOnlySpan<Type> componentTypes, int count)
    {
        if (componentTypes.Length == 0 || componentTypes.Length > 16)
            throw new ArgumentException("1-16 components per entity only", nameof(componentTypes));
        Archetype archetype = Archetype.CreateOrGetExistingArchetype(componentTypes, this);
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
}
