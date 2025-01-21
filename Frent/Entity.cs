using Frent.Core;
using Frent.Updating;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// Represents an Entity; a collection of components of unqiue type
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

    internal byte WorldVersion;
    internal byte WorldID;
    internal ushort EntityVersion;
    internal int EntityID;
    #endregion

    #region Internal Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsAlive([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
        if (World.WorldCachePackedValue == Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).PackedWorldInfo)
        {
            world = World.QuickWorldCache;
            var tableItem = world!.EntityTable[(uint)EntityID];
            entityLocation = tableItem.Location;
            return tableItem.Version == EntityVersion;
        }
        return IsAliveCold(out world, out entityLocation);
    }

    private bool IsAliveCold([NotNullWhen(true)] out World? world, out EntityLocation entityLocation)
    {
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
                    //refresh the cache
                    World.WorldCachePackedValue = PackedWorldInfo;
                    World.QuickWorldCache = world;

                    entityLocation = loc;
                    return true;
                }

            }
        }

        entityLocation = default;
        world = null;
        return false;
    }

    private Ref<T> TryGetCore<T>(out bool exists)
    {
        if(!IsAlive(out var world, out var entityLocation))
        {
            exists = false;
            return default;
        }

        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
        {
            exists = false;
            return default;
        }

        exists = true;
        return Ref<T>.Create(((IComponentRunner<T>)entityLocation.Archetype(world).Components[compIndex]).AsSpan()[entityLocation.ChunkIndex].AsSpan(), entityLocation.ComponentIndex);
    }

    internal static Ref<TComp> GetComp<TComp>(scoped ref readonly EntityLocation entityLocation, Archetype archetype)
    {
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<TComp>.ID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException(typeof(TComp));

        return Ref<TComp>.Create(((IComponentRunner<TComp>)archetype.Components[compIndex]).AsSpan()[entityLocation.ChunkIndex].AsSpan(), entityLocation.ComponentIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertIsAlive(out World world, out EntityLocation entityLocation)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            var tableItem = (world = World.QuickWorldCache!).EntityTable.GetValueNoCheck(EntityID);
            if (tableItem.Version == EntityVersion)
            {
                entityLocation = tableItem.Location;
                return;
            }
        }

        if (!IsAliveCold(out world!, out entityLocation))
            Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertIsAliveWorld(out World world)
    {
        if (!(World.WorldCachePackedValue == PackedWorldInfo &&
            (world = World.QuickWorldCache!).EntityTable.GetValueNoCheck(EntityID).Version == EntityVersion ||
            IsAliveCold(out world!, out _)))
        {
            Throw_EntityIsDead();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertIsAliveEntityLocation(out EntityLocation eloc)
    {
        if (World.WorldCachePackedValue == PackedWorldInfo)
        {
            var tableItem = World.QuickWorldCache!.EntityTable.GetValueNoCheck(EntityID);
            if (tableItem.Version == EntityVersion)
            {
                eloc = tableItem.Location;
                return;
            }
        }

        eloc = default;
        Throw_EntityIsDead();
    }

    private void AssertIsAlive()
    {
        if (!IsAlive())
            Throw_EntityIsDead();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Throw_EntityIsDead() => FrentExceptions.Throw_InvalidOperationException(EntityIsDeadMessage);

    //captial N null to distinguish between actual null and default
    internal string DebuggerDisplayString => IsNull ? "Null" : IsAlive() ? $"World: {WorldID}, World Version: {WorldVersion}, ID: {EntityID}, Version {EntityVersion}" : EntityIsDeadMessage;
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
                if (!target.IsAlive(out World? world, out var eloc))
                    return Array.Empty<object>();

                object[] objects = new object[ComponentTypes.Length];
                Archetype archetype = eloc.Archetype(world);
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i] = archetype.Components[i].GetAt(eloc.ChunkIndex, eloc.ComponentIndex);
                }

                return objects;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityWorldInfoAccess
    {
        internal ushort PackedWorldInfo;
        internal ushort EntityVersion;
        internal int EntityID;
    }

    internal ushort PackedWorldInfo => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).PackedWorldInfo;
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
    public override int GetHashCode() => HashCode.Combine(WorldID, EntityVersion, EntityID);
    #endregion
}