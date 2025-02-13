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

    #region Internal Helpers

    #region IsAlive
    internal bool InternalIsAlive([NotNullWhen(true)] out World world, out EntityLocation entityLocation)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        if (world is null)
        {
            Unsafe.SkipInit(out entityLocation);
            return false;
        }
        ref World.EntityLookup lookup = ref world.EntityTable.UnsafeIndexNoResize(EntityID);
        entityLocation = lookup.Location;
        return lookup.Version == EntityVersion;
    }

    internal void AssertIsAlive(out World world, out EntityLocation entityLocation)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        //hardware trap
        entityLocation = world.EntityTable.UnsafeIndexNoResize(EntityID).Location;
    }

    #endregion IsAlive

    private Ref<T> TryGetCore<T>(out bool exists)
    {
        if (!InternalIsAlive(out var world, out var entityLocation))
            goto doesntExist;

        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex == 0)
            goto doesntExist;

        exists = true;
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(
            entityLocation.Archetype.Components.UnsafeArrayIndex(compIndex));

        return new Ref<T>(ref storage[entityLocation.Index]);

    doesntExist:
        exists = false;
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Ref<TComp> GetComp<TComp>(scoped ref readonly EntityLocation entityLocation, IComponentRunner[] archetypeComponents)
    {
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<TComp>.ID);

        if (compIndex == 0)
            FrentExceptions.Throw_ComponentNotFoundException(typeof(TComp));

        ComponentStorage<TComp> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<TComp>>(
            archetypeComponents.UnsafeArrayIndex(compIndex));

        return new Ref<TComp>(ref storage[entityLocation.Index]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Throw_EntityIsDead() => throw new InvalidOperationException(EntityIsDeadMessage);

    //captial N null to distinguish between actual null and default
    internal string DebuggerDisplayString => IsNull ? "Null" : InternalIsAlive(out _, out _) ? $"World: {WorldID}, ID: {EntityID}, Version {EntityVersion}" : EntityIsDeadMessage;
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
                Archetype archetype = eloc.Archetype;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct EntityHighLow
    {
        internal int EntityID;
        internal int EntityLow;
    }

    internal ushort PackedWorldInfo => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).PackedWorldInfo;
    internal EntityIDOnly EntityIDOnly => Unsafe.As<Entity, EntityWorldInfoAccess>(ref this).EntityIDOnly;
    internal long PackedValue => Unsafe.As<Entity, long>(ref this);
    internal int EntityLow => Unsafe.As<Entity, EntityHighLow>(ref this).EntityLow;
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