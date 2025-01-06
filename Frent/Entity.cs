using Frent.Core;
using Frent.Updating;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// Represents an Entity; a collection of components of unqiue type
/// </summary>
//TODO: comparison with fieldoffset
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
public readonly partial struct Entity : IEquatable<Entity>
{
    #region Fields & Ctor
    internal Entity(byte worldID, byte worldVersion, ushort version, int entityID)
    {
        WorldID = worldID;
        WorldVersion = worldVersion;
        EntityVersion = version;
        EntityID = entityID;
    }

    internal readonly byte WorldVersion;
    internal readonly byte WorldID;
    internal readonly ushort EntityVersion;
    internal readonly int EntityID;
    #endregion

    #region Public API

    #region Has
    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public bool Has<T>()
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        ComponentID compid = Component<T>.ID;
        return GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, compid) < PreformanceHelpers.MaxComponentCount;
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <paramref name="type"/>
    /// </summary>
    /// <param name="type">The component type to check if this entity has</param>
    /// <returns><see langword="true"/> if the entity has a component of <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public bool Has(Type type)
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        ComponentID compid = Component.GetComponentID(type);
        return GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, compid) < PreformanceHelpers.MaxComponentCount;
    }
    #endregion

    #region Get
    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException{T}"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>A reference to the component in memory.</returns>
    public ref T Get<T>()
    {
        //Total: 7x dereference

        //2x
        if (!IsAlive(out var world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        //2x
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex >= PreformanceHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException<T>();
        //3x
        return ref ((IComponentRunner<T>)entityLocation.Archetype(world).Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
    }

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException{T}"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component of type <paramref name="type"/></returns>
    public object Get(Type type)
    {
        if (!IsAlive(out var world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        //2x
        ComponentID compid = Component.GetComponentID(type);
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, compid);

        if (compIndex >= PreformanceHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException(type);
        //3x
        return entityLocation.Archetype(world).Components[compIndex].GetAt(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
    }
    #endregion

    #region TryGet
    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if this entity has a component of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public bool TryGet<T>(out Ref<T> value)
    {
        value = TryGetCore<T>(out bool exists)!;
        return exists;
    }

    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="exists"><see langword="true"/> if this entity has a component of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</param>
    /// <returns>Potentially a reference to the component</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ref T? TryGet<T>(out bool exists)
    {
        var @ref = TryGetCore<T>(out exists);
        return ref @ref.Component!;
    }

    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <param name="type">The type of component to try and get</param>
    /// <returns><see langword="true"/> if this entity has a component of type <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public bool TryGet(Type type, [NotNullWhen(true)] out object? value)
    {
        if (!IsAlive(out World? world, out EntityLocation entityLocation))
        {
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        }

        ComponentID componentId = Component.GetComponentID(type);
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, componentId.ID);

        if (compIndex >= PreformanceHelpers.MaxComponentCount)
        {
            value = null;
            return false;
        }

        value = entityLocation.Archetype(world).Components[compIndex].GetAt(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        return true;
    }
    #endregion

    #region Add
    /// <summary>
    /// Adds a component to an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <param name="component">The component instance to add</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentAlreadyExistsException{T}"><see cref="Entity"/> already has a component of type <typeparamref name="T"/></exception>
    public void Add<T>(in T component)
    {
        AddCore(Component<T>.ID, typeof(T), out IComponentRunner to, out EntityLocation location);
        ((IComponentRunner<T>)to).AsSpan()[location.ChunkIndex][location.ComponentIndex] = component;
    }

    /// <summary>
    /// Add a component to an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type to add the component as. Note that a component of type DerivedClass and BaseClass are different component types.</param>
    /// <param name="component">The component to add</param>
    public void Add(Type type, object component)
    {
        if (!component.GetType().IsAssignableTo(type))
            throw new ArgumentException("Component must be assignable to the given component type!", nameof(component));

        AddCore(Component.GetComponentID(type), type, out IComponentRunner to, out EntityLocation location);
        to.SetAt(component, location.ChunkIndex, location.ComponentIndex);
    }
    #endregion

    #region Remove
    /// <summary>
    /// Removes a component from an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>The component that was removed</returns>
    public T Remove<T>()
    {
        RemoveCore(Component<T>.ID, typeof(T), out var nextLocation, out var entityLocation, out int skipIndex, out World world);

        T result = default!;
        int j = 0;
        Archetype fromArchetype = entityLocation.Archetype(world);
        Archetype nextArchetype = nextLocation.Archetype(world);

        for (int i = 0; i < fromArchetype.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                result = ((IComponentRunner<T>)fromArchetype.Components[i]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
                continue;
            }
            nextArchetype.Components[j++].PullComponentFrom(fromArchetype.Components[i], nextLocation, entityLocation);
        }

        Entity movedDown = fromArchetype.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;
        return result;
    }

    /// <summary>
    /// Removes a component from an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type of component to remove</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component that was removed</returns>
    public object Remove(Type type)
    {
        RemoveCore(Component.GetComponentID(type), type, out var nextLocation, out var entityLocation, out int skipIndex, out World world);

        object result = default!;
        int j = 0;
        Archetype fromArchetype = entityLocation.Archetype(world);
        Archetype nextArchetype = nextLocation.Archetype(world);

        for (int i = 0; i < fromArchetype.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                result = fromArchetype.Components[i].GetAt(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
                continue;
            }
            nextArchetype.Components[j++].PullComponentFrom(nextArchetype.Components[i], nextLocation, entityLocation);
        }

        Entity movedDown = fromArchetype.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;

        return result;
    }
    #endregion

    #region Tag
    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a generic type parameter to represent the tag.
    /// </summary>
    /// <typeparam name="T">The type used as the tag.</typeparam>
    /// <returns>
    /// <see langword="true"/> if the tag of type <typeparamref name="T"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> is not alive.</exception>
    public bool Tagged<T>()
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        TagID tagid = Core.Tag<T>.ID;
        return GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagid);
    }

    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a <see cref="TagID"/> to represent the tag.
    /// </summary>
    /// <param name="tagID">The identifier of the tag to check.</param>
    /// <returns>
    /// <see langword="true"/> if the tag identified by <paramref name="tagID"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> is not alive.</exception>
    public bool Tagged(TagID tagID)
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        return GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID);
    }

    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a <see cref="Type"/> to represent the tag.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tagged(TagID)"/> or <see cref="Tagged{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <param name="type">The <see cref="Type"/> representing the tag to check.</param>
    /// <returns>
    /// <see langword="true"/> if the tag represented by <paramref name="type"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> not alive.</exception>
    public bool Tagged(Type type)
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        TagID tagid = Core.Tag.GetTagID(type);
        return GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagid);
    }

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <typeparam name="T">The type to use as the tag.</typeparam>
    public void Tag<T>() => TagCore(Core.Tag<T>.ID, typeof(T));
    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="type">The type to use as a tag</param>
    public void Tag(Type type) => TagCore(Core.Tag.GetTagID(type), type);
    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tag(TagID)"/> or <see cref="Tag{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="tagID">The tagID to use as the tag</param>
    public void Tag(TagID tagID) => TagCore(tagID, tagID.Type);
    #endregion

    #region Detach
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <typeparam name="T">The type of tag to remove.</typeparam>
    public bool Detach<T>() => DetachCore(Core.Tag<T>.ID, typeof(T));
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="type">The type of tag to remove.</param>
    public bool Detach(Type type) => DetachCore(Core.Tag.GetTagID(type), type);
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="tagID">The type of tag to remove.</param>
    public bool Detach(TagID tagID) => DetachCore(tagID, tagID.Type);
    #endregion

    #region Misc
    /// <summary>
    /// Deletes this entity
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public void Delete()
    {
        if (IsAlive(out World? world, out EntityLocation entityLocation))
        {
            world.DeleteEntityInternal(this, ref entityLocation);
        }
        else
        {
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        }
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> is still alive
    /// </summary>
    /// <returns><see langword="true"/> if this entity is still alive (not deleted), otherwise <see langword="false"/></returns>
    public bool IsAlive() => IsAlive(out _, out _);

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> instance is the null entity: <see langword="default"/>(<see cref="Entity"/>)
    /// </summary>
    public bool IsNull => WorldID == 0 && WorldVersion == 0 && EntityID == 0 && EntityVersion == 0;

    /// <summary>
    /// Gets the world this entity belongs to
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public World World
    {
        get
        {
            if (!IsAlive(out World? world, out _))
                FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
            return world;
        }
    }

    /// <summary>
    /// Gets the component types for this entity, ordered in update order
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ImmutableArray<Type> ComponentTypes
    {
        get
        {
            if (!IsAlive(out World? world, out EntityLocation loc))
                FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
            return loc.Archetype(world).ArchetypeTypeArray;
        }
    }

    /// <summary>
    /// Gets tags the entity has 
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ImmutableArray<Type> TagTypes
    {
        get
        {
            if (!IsAlive(out World? world, out EntityLocation loc))
                FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
            return loc.Archetype(world).ArchetypeTagArray;
        }
    }
    #endregion

    #endregion

    #region Internal Helpers
    internal bool IsAlive([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        //2x dereference
        var span = GlobalWorldTables.Worlds.AsSpan();
        int worldId = WorldID;
        if (span.Length > worldId)
        {
            world = span[worldId];
            if (world.Version == WorldVersion)
            {
                var (loc, ver) = world.EntityTable[(uint)EntityID];
                if (ver == EntityVersion)
                {
                    entityLocation = loc;
                    return true;
                }

            }
        }

        entityLocation = default;
        world = null;
        return false;
    }

    private void AddCore(ComponentID componentID, Type type, out IComponentRunner lastTo, out EntityLocation nextLocation)
    {
        //this code is similar to TagCore
        if (!IsAlive(out World? world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        Archetype from = entityLocation.Archetype(world);

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, componentID.ID, out _);

        Archetype destination = edge.Add ??= Archetype.CreateOrGetExistingArchetype(Concat(from.ArchetypeTypeArray, type, out var res), from.ArchetypeTagArray.AsSpan(), world, res, from.ArchetypeTagArray);
        destination.CreateEntityLocation(out nextLocation) = this;

        for (int i = 0; i < from.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        lastTo = destination.Components[^1];

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;
    }
    private Ref<T> TryGetCore<T>(out bool exists)
    {
        if (!IsAlive(out var world, out EntityLocation entityLocation))
        {
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        }

        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex >= PreformanceHelpers.MaxComponentCount)
        {
            exists = false;
            return default;
        }

        exists = true;
        return Ref<T>.Create(((IComponentRunner<T>)entityLocation.Archetype(world).Components[compIndex]).AsSpan()[entityLocation.ChunkIndex].AsSpan(), entityLocation.ComponentIndex);
    }

    private void RemoveCore(ComponentID componentID, Type type, out EntityLocation nextLocation, out EntityLocation entityLocation, out int skipIndex, out World world)
    {
        //ignore - it throws if null
        if (!IsAlive(out world!, out entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        //this method is similar to DetachCore
        Archetype from = entityLocation.Archetype(world);
        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, componentID.ID, out _);
        Archetype destination = edge.Remove ??= Archetype.CreateOrGetExistingArchetype(Remove(from.ArchetypeTypeArray, type, out var arr), from.ArchetypeTagArray.AsSpan(), world, arr, from.ArchetypeTagArray);

        destination.CreateEntityLocation(out nextLocation) = this;

        skipIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, componentID);
        if (skipIndex >= PreformanceHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException(type);
    }

    internal static Ref<TComp> GetComp<TComp>(scoped ref readonly EntityLocation entityLocation, Archetype archetype)
    {
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<TComp>.ID);

        if (compIndex >= PreformanceHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException<TComp>();

        return Ref<TComp>.Create(((IComponentRunner<TComp>)archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex].AsSpan(), entityLocation.ComponentIndex);
    }

    private static ReadOnlySpan<Type> Concat(ImmutableArray<Type> types, Type type, out ImmutableArray<Type> result)
    {
        if (types.IndexOf(type) != -1)
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {type.Name}");

        var builder = ImmutableArray.CreateBuilder<Type>(types.Length + 1);
        builder.AddRange(types);
        builder.Add(type);

        result = builder.MoveToImmutable();
        return result.AsSpan();
    }

    private static ReadOnlySpan<Type> Remove(ImmutableArray<Type> types, Type type, out ImmutableArray<Type> result)
    {
        int index = types.IndexOf(type);
        if (index == -1)
            FrentExceptions.Throw_ComponentNotFoundException(type);
        result = types.RemoveAt(index);
        return result.AsSpan();
    }

    private static ReadOnlySpan<Type> RemoveAt(ImmutableArray<Type> types, int index, out ImmutableArray<Type> result)
    {
        result = types.RemoveAt(index);
        return result.AsSpan();
    }

    private void TagCore(TagID tagID, Type type)
    {
        //this code is similar to AddCore
        if (!IsAlive(out World? world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        Archetype from = entityLocation.Archetype(world);

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, tagID.ID, out _);

        Archetype destination = edge.AddTag ??= Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Concat(from.ArchetypeTagArray, type, out var res), world, from.ArchetypeTypeArray, res);
        destination.CreateEntityLocation(out var nextLocation) = this;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;
    }

    private bool DetachCore(TagID tagID, Type type)
    {
        //this method is similar to RemoveCore

        //ignore - it throws if null
        if (!IsAlive(out var world, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        if (!GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID))
            return false;

        Archetype from = entityLocation.Archetype(world);
        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, tagID.ID, out _);

        int indexToRemoveAt = from.ArchetypeTagArray.IndexOf(type);

        Archetype destination = edge.Remove ??= Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Remove(from.ArchetypeTagArray, type, out var arr), world, from.ArchetypeTypeArray, arr);

        destination.CreateEntityLocation(out var nextLocation) = this;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for(int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;

        return true;
    }

    internal string DebuggerDisplayString => IsNull ? "null" : $"World: {WorldID}, World Version: {WorldVersion}, ID: {EntityID}, Version {EntityVersion}";
    internal const string EntityIsDeadMessage = "Entity is Dead";
    internal const string DoesNotHaveTagMessage = "This Entity does not have this tag";
    #endregion

    #region IEquatable
    /// <summary>
    /// Checks if two <see cref="Entity"/> structs refer to the same entity.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities refer to the same entity; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Entity a, Entity b) => a.Equals(b);

    /// <summary>
    /// Checks if two <see cref="Entity"/> structs do not refer to the same entity.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities do not refer to the same entity; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Entity"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns><see langword="true"/> if the specified object is an <see cref="Entity"/> and is equal to the current entity; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    /// <summary>
    /// Determines whether the specified <see cref="Entity"/> is equal to the current <see cref="Entity"/>.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns><see langword="true"/> if the specified entity is equal to the current entity; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Entity other) => other.WorldID == WorldID && other.EntityVersion == EntityVersion && other.EntityID == EntityID;

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current <see cref="Entity"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(WorldID, EntityVersion, EntityID);
    #endregion
}