using Frent.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Frent.Updating;

namespace Frent;

/// <summary>
/// Represents an Entity; a collection of components of unqiue type
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
public readonly partial struct Entity : IEquatable<Entity>
{
    internal Entity(byte worldID, byte worldVersion, ushort version, int entityID)
    {
        WorldID = worldID;
        WorldVersion = worldVersion;
        EntityVersion = version;
        EntityID = entityID;
    }

    #region Fields
    internal readonly byte WorldVersion;
    internal readonly byte WorldID;
    internal readonly ushort EntityVersion;
    internal readonly int EntityID;
    #endregion

    #region Interactions
    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public bool Has<T>()
    {
        if (!IsAlive(out _, out var entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        int compid = Component<T>.ID;
        return GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][compid] != byte.MaxValue;
    }

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <typeparamref name="T"/>,
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException{T}"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>A reference to the component in memory.</returns>
    public ref T Get<T>()
    {
        //Total: 7x dereference

        //2x
        if (!IsAlive(out _, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        //5x
        byte compIndex = GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][Component<T>.ID];

        if (compIndex == byte.MaxValue)
            FrentExceptions.Throw_ComponentNotFoundException<T>();

        return ref ((IComponentRunner<T>)entityLocation.Archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
    }

    /// <summary>
    /// Attempts to get a component reference from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component to try get.</typeparam>
    /// <returns>An <see cref="Option{T}"/> that might contain a component reference.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public Option<T> TryGet<T>()
    {
        ref T? value = ref TryGetCore<T>(out bool exists);
        //this can only be null if the user set something to be null
        return new Option<T>(exists, ref value!);
    }

    /// <summary>
    /// Attempts to get a component reference from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if this entity has a component of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public bool TryGet<T>(out Ref<T> value)
    {
        value = new Ref<T>(ref TryGetCore<T>(out bool exists)!);
        return exists;
    }

    /// <summary>
    /// Adds a component to an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <param name="component">The component instance to add</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentAlreadyExistsException{T}"><see cref="Entity"/> already has a component of type <typeparamref name="T"/></exception>
    public void Add<T>(in T component)
    {
        if(!IsAlive(out World? world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        Archetype from = entityLocation.Archetype;

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, Component<T>.ID, out _);

        Archetype destination = edge.Add ??= Archetype.CreateOrGetExistingArchetype(Concat(from.ArchetypeTypeArray, typeof(T)), world);
        destination.CreateEntityLocation(out EntityLocation nextLocation) = this;

        for(int i = 0; i < from.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(from.Components[i], ref nextLocation, ref entityLocation);
        }

        IComponentRunner<T> last = (IComponentRunner<T>)destination.Components[^1];
        last.AsSpan()[nextLocation.ChunkIndex][nextLocation.ComponentIndex] = component;

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;

        static Type[] Concat(Type[] types, Type type)
        {
            if (Array.IndexOf(types, type) != -1)
                FrentExceptions.Throw_ComponentAlreadyExistsException<T>();
            Type[] arr = new Type[types.Length + 1];
            types.CopyTo(arr, 0);
            arr[^1] = type;
            return arr;
        }
    }

    /// <summary>
    /// Removes a component from an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException{T}"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>The component that was removed</returns>
    public T Remove<T>()
    {
        if (!IsAlive(out World? world, out EntityLocation entityLocation))
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

        Archetype from = entityLocation.Archetype;

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(from.Graph, Component<T>.ID, out _);
        Archetype destination = edge.Remove ??= Archetype.CreateOrGetExistingArchetype(Remove(from.ArchetypeTypeArray, typeof(T)), world);

        destination.CreateEntityLocation(out EntityLocation nextLocation) = this;

        int skipIndex = GlobalWorldTables.ComponentLocationTable[from.ArchetypeID][Component<T>.ID];
        int j = 0;

        if (skipIndex == byte.MaxValue)
            FrentExceptions.Throw_ComponentNotFoundException<T>();

        T result = default!;
        for (int i = 0; i < from.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                result = ((IComponentRunner<T>)from.Components[i]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
                continue;
            }
            destination.Components[j++].PullComponentFrom(from.Components[i], ref nextLocation, ref entityLocation);
        }

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        world.EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        world.EntityTable[(uint)EntityID].Location = nextLocation;

        return result;

        static Type[] Remove(Type[] types, Type type)
        {
            Type[] arr = new Type[types.Length - 1];
            int j = 0;

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == type)
                    continue;

                arr[j++] = types[i];
            }

            return arr;
        }
    }

    /// <summary>
    /// Deletes this entity
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public void Delete()
    {
        if(IsAlive(out World? world, out EntityLocation entityLocation))
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
    #endregion

    #region Private Helpers
    internal bool IsAlive([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        //2x dereference
        var span = GlobalWorldTables.Worlds.AsSpan();
        int worldId = WorldID;
        if (span.Length > worldId)
        {
            world = span[worldId];
            if(world.Version == WorldVersion)
            {
                var (loc, ver) = world.EntityTable[(uint)EntityID];
                if(ver == EntityVersion)
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

    private ref T? TryGetCore<T>(out bool exists)
    {
        if (!IsAlive(out _, out EntityLocation entityLocation))
        {
            FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);
        }

        byte compIndex = GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][Component<T>.ID];

        if (compIndex == byte.MaxValue)
        {
            exists = false;
            return ref DefaultReference<T>.Value;
        }

        exists = true;
        return ref ((IComponentRunner<T>)entityLocation.Archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex]!;
    }


    internal static Ref<TComp> GetComp<TComp>(scoped ref readonly EntityLocation entityLocation)
    {
        byte compIndex = GlobalWorldTables.ComponentLocationTable[entityLocation.Archetype.ArchetypeID][Component<TComp>.ID];

        if (compIndex == byte.MaxValue)
            FrentExceptions.Throw_ComponentNotFoundException<TComp>();

        return new(ref ((IComponentRunner<TComp>)entityLocation.Archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex]);
    }

    internal string DebuggerDisplayString => IsNull ? "null" : $"World: {WorldID}, World Version: {EntityVersion}, ID: {EntityID}, Version {EntityVersion}";
    internal const string EntityIsDeadMessage = "Entity is Dead";
    #endregion

    #region IEquatable
    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    public bool Equals(Entity other) => other.WorldID == WorldID && other.EntityVersion == EntityVersion && other.EntityID == EntityID;
    public override int GetHashCode() => HashCode.Combine(WorldID, EntityVersion, EntityID);
    #endregion

    private static class DefaultReference<T> { public static T? Value; }
}