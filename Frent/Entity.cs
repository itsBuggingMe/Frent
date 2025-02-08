using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// An Entity reference; refers to a collection of components of unqiue types.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay(AttributeHelpers.DebuggerDisplay)]
[DebuggerTypeProxy(typeof(EntityDebugView))]
public partial struct Entity : IEquatable<Entity>
{
    #region Fields & Ctor
    /// <summary>
    /// Creates an <see cref="Entity"/> identical to <see cref="Entity.Null"/>
    /// </summary>
    /// <remarks><see cref="Entity"/> generally shouldn't manually constructed</remarks>
    public Entity() { }

    internal Entity(byte worldID, byte worldVersion, ushort version, int entityID)
    {
        WorldID = worldID;
        WorldVersion = worldVersion;
        EntityVersion = version;
        EntityID = entityID;
    }

    //WARNING
    //DO NOT CHANGE STRUCT LAYOUT
    internal int EntityID;
    internal ushort EntityVersion;
    internal byte WorldVersion;
    internal byte WorldID;
    #endregion

    #region Internal Helpers

    #region IsAlive
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalIsAlive([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            world = World.QuickWorldCache;
            var tableItem = world!.EntityTable.UnsafeIndexNoResize(EntityID);
            entityLocation = tableItem.Location;
            return tableItem.Version == EntityVersion;
        }
        return IsAliveCold(out world, out entityLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalIsAlive([NotNullWhen(true)] out World? world)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            world = World.QuickWorldCache;
            return world!.EntityTable[EntityID].Version == EntityVersion;
        }
        return IsAliveCold(out world, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalIsAlive(out EntityLocation entityLocation)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            var tableItem = World.QuickWorldCache!.EntityTable[EntityID];
            entityLocation = tableItem.Location;
            return tableItem.Version == EntityVersion;
        }
        return IsAliveCold(out _, out entityLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalIsAlive()
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            return World.QuickWorldCache!.EntityTable[EntityID].Version == EntityVersion;
        }
        return IsAliveCold(out _, out _);
    }

    internal bool IsAliveCold([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        world = GlobalWorldTables.Worlds.GetValueNoCheck(WorldID);
        if (world?.Version == WorldVersion)
        {
            var (loc, ver) = world.EntityTable[EntityID];
            if (ver == EntityVersion)
            {
                //refresh the cache
                World.WorldCachePackedValue = PackedWorldInfo;
                World.QuickWorldCache = world;

                entityLocation = loc;
                return true;
            }

        }

        entityLocation = default;
        world = null;
        return false;
    }

    #endregion IsAlive

    #region AssertIsAlive
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssertIsAlive(out World world, out EntityLocation entityLocation)
    {
        if (InternalIsAlive(out world!, out entityLocation))
            return;
        Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssertIsAliveDum(out World world, out World.EntityLookup entityLocation)
    {
        if (InternalIsAliveDum(out world!, out entityLocation))
            return;
        Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalIsAliveDum([NotNullWhen(true)] out World? world, out World.EntityLookup entityLocation)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            world = World.QuickWorldCache;
            entityLocation = world!.EntityTable.UnsafeIndexNoResize(EntityID);
            return entityLocation.Version == EntityVersion;
        }
        return IsAliveColdDum(out world, out entityLocation);
    }

    internal bool IsAliveColdDum([NotNullWhen(true)] out World? world, out World.EntityLookup entityLocation)
    {
        world = GlobalWorldTables.Worlds.GetValueNoCheck(WorldID);
        if (world?.Version == WorldVersion)
        {
            World.EntityLookup e = world.EntityTable[EntityID];
            if (e.Version == EntityVersion)
            {
                //refresh the cache
                World.WorldCachePackedValue = PackedWorldInfo;
                World.QuickWorldCache = world;

                entityLocation = e;
                return true;
            }

        }

        entityLocation = default;
        world = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssertIsAlive(out World world)
    {
        if (InternalIsAlive(out world!))
            return;
        Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssertIsAlive(out EntityLocation entityLocation)
    {
        if (InternalIsAlive(out entityLocation))
            return;
        Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssertIsAlive()
    {
        if (InternalIsAlive())
            return;
        Throw_EntityIsDead();
    }
    #endregion AssertIsAlive

    private Ref<T> TryGetCore<T>(out bool exists)
    {
        if (!InternalIsAlive(out var world, out var entityLocation))
            goto doesntExist;

        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            goto doesntExist;

        exists = true;
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(
            entityLocation.Archetype(world).Components.UnsafeArrayIndex(compIndex));

        return new Ref<T>(ref storage[entityLocation.Index]);

    doesntExist:
        exists = false;
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Ref<TComp> GetComp<TComp>(scoped ref readonly EntityLocation entityLocation, IComponentRunner[] archetypeComponents)
    {
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<TComp>.ID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException(typeof(TComp));

        ComponentStorage<TComp> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<TComp>>(
            archetypeComponents.UnsafeArrayIndex(compIndex));

        return new Ref<TComp>(ref storage[entityLocation.Index]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Throw_EntityIsDead() => throw new InvalidOperationException(EntityIsDeadMessage);

    //captial N null to distinguish between actual null and default
    internal string DebuggerDisplayString => IsNull ? "Null" : InternalIsAlive() ? $"World: {WorldID}, World Version: {WorldVersion}, ID: {EntityID}, Version {EntityVersion}" : EntityIsDeadMessage;
    internal const string EntityIsDeadMessage = "Entity is Dead";
    internal const string DoesNotHaveTagMessage = "This Entity does not have this tag";

    private class EntityDebugView(Entity target)
    {
        public ImmutableArray<ComponentID> ComponentTypes => target.ComponentTypes;
        public ImmutableArray<TagID> Tags => target.TagTypes;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Components
        {
            get
            {
                if (!target.InternalIsAlive(out World? world, out var eloc))
                    return Array.Empty<object>();

                object[] objects = new object[ComponentTypes.Length];
                Archetype archetype = eloc.Archetype(world);
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i] = archetype.Components[i].GetAt(eloc.Index);
                }

                return objects;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityWorldInfoAccess
    {
        internal EntityIDOnly EntityIDOnly;
        internal ushort PackedWorldInfo;
    }

    internal ushort PackedWorldInfo => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).PackedWorldInfo;
    internal EntityIDOnly EntityIDOnly => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).EntityIDOnly;
    internal long PackedValue => Unsafe.As<Entity, long>(ref this);
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
    public bool Equals(Entity other) => other.PackedValue == PackedValue;

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current <see cref="Entity"/>.</returns>
    public override int GetHashCode() => PackedValue.GetHashCode();
    #endregion
}