using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating.Runners;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// An Entity reference; refers to a collection of components of unqiue types.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
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

    internal Entity(ushort worldID, ushort version, int entityID)
    {
        WorldID = worldID;
        EntityVersion = version;
        EntityID = entityID;
    }

    //WARNING
    //DO NOT CHANGE STRUCT LAYOUT
    internal int EntityID;
    internal ushort EntityVersion;
    internal ushort WorldID;
    #endregion

    #region Props
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityWorldInfoAccess
    {
        internal EntityIDOnly EntityIDOnly;
        internal ushort WorldID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityHighLow
    {
        internal int EntityID;
        internal int EntityLow;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityData
    {
        internal int EntityID;
        internal ushort EntityVersion;
        internal ushort WorldID;
    }

    internal EntityIDOnly EntityIDOnly => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).EntityIDOnly;
    internal long PackedValue => Unsafe.As<Entity, long>(ref this);
    internal int EntityLow => Unsafe.As<Entity, EntityHighLow>(ref this).EntityLow;
    #endregion

    #region Internal Helpers

    #region IsAlive
    internal bool InternalIsAlive([NotNullWhen(true)] out World world, out EntityLocation entityLocation)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        if (world is null)
        {
            entityLocation = default;
            return false;
        }
        entityLocation = world.EntityTable.UnsafeIndexNoResize(EntityID);
        return entityLocation.Version == EntityVersion;
    }

    /// <exception cref="InvalidOperationException">This <see cref="Entity"/> has been deleted.</exception>
    internal ref EntityLocation AssertIsAlive(out World world)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        //hardware trap
        ref var lookup = ref world.EntityTable.UnsafeIndexNoResize(EntityID);
        if (lookup.Version != EntityVersion)
            Throw_EntityIsDead();
        return ref lookup;
    }

    #endregion IsAlive

    private Ref<T> TryGetCore<T>(out bool exists)
    {
        if (!InternalIsAlive(out var _, out var entityLocation))
            goto doesntExist;

        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex == 0)
            goto doesntExist;

        exists = true;
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(
            entityLocation.Archetype.Components.UnsafeArrayIndex(compIndex));

        return new Ref<T>(storage, entityLocation.Index);

    doesntExist:
        exists = false;
        return default;
    }

    private static void Throw_EntityIsDead() => throw new InvalidOperationException(EntityIsDeadMessage);

    //captial N null to distinguish between actual null and default
    internal string DebuggerDisplayString => IsNull ? "Null" : InternalIsAlive(out _, out _) ? $"World: {WorldID}, ID: {EntityID}, Version {EntityVersion}" : EntityIsDeadMessage;
    internal const string EntityIsDeadMessage = "Entity is dead.";
    internal const string DoesNotHaveTagMessage = "This entity does not have this tag";

    private class EntityDebugView(Entity target)
    {
        public ImmutableArray<ComponentID> ComponentTypes => target.ComponentTypes;
        public ImmutableArray<TagID> Tags => target.TagTypes;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Dictionary<Type, object> Components
        {
            get
            {
                if (!target.InternalIsAlive(out World? world, out var eloc))
                    return [];

                Dictionary<Type, object> components = [];

                for (int i = 0; i < ComponentTypes.Length; i++)
                {
                    components[ComponentTypes[i].Type] = target.Get(ComponentTypes[i]);
                }

                return components;
            }
        }
    }
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