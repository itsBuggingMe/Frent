using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Core.Events;
using Frent.Updating;
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

    internal readonly EntityIDOnly EntityIDOnly => Unsafe.As<Entity, EntityWorldInfoAccess>(ref Unsafe.AsRef(in this)).EntityIDOnly;
    internal readonly long PackedValue => Unsafe.As<Entity, long>(ref Unsafe.AsRef(in this));
    internal readonly int EntityLow => Unsafe.As<Entity, EntityHighLow>(ref Unsafe.AsRef(in this)).EntityLow;
    #endregion

    #region Internal Helpers

    #region IsAlive
    internal readonly ref EntityLocation InternalIsAlive(out World world, out bool exists)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        if (world is null)
        {
            exists = false;
            return ref Unsafe.NullRef<EntityLocation>();
        }
        ref EntityLocation entityLocation = ref world.EntityTable.UnsafeIndexNoResize(EntityID);
        exists = entityLocation.Version == EntityVersion;
        return ref entityLocation;
    }

    /// <exception cref="InvalidOperationException">This <see cref="Entity"/> has been deleted.</exception>
    internal readonly ref EntityLocation AssertIsAlive(out World world)
    {
        world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        //hardware trap
        ref var lookup = ref world.EntityTable.UnsafeIndexNoResize(EntityID);
        if (lookup.Version != EntityVersion)
            Throw_EntityIsDead();
        return ref lookup;
    }

    #endregion IsAlive

    /// <summary>
    /// Assumes both this entity and the target are alive and belong to <paramref name="world"/>.
    /// </summary>
    private readonly bool LinkCore(LinkID linkKind, ref EntityLocation eloc, Entity target, ref EntityLocation targetEloc, World world)
    {
        ref LinkTable links = ref world.WorldLinkTable.UnsafeArrayIndex(linkKind.RawValue);

        Archetype sourceArchetype = eloc.Archetype;
        Archetype targetArchetype = targetEloc.Archetype;

        int sourceLinkId = sourceArchetype.GetExistingOrCreateLinkID(world, eloc.Index);
        int targetLinkId = targetArchetype.GetExistingOrCreateLinkID(world, targetEloc.Index);

        eloc.Flags |= EntityFlags.HasHadOutgoingLinks;
        targetEloc.Flags |= EntityFlags.HasHadIncomingLinks;

        // record the outgoing link on this entity: this -> target
        ref LinkTableEntry outgoing = ref MemoryHelpers.GetValueOrResize(ref links.OutgoingLinks, sourceLinkId);
        if (!outgoing.AddLink(targetArchetype, targetEloc.Index))
            return false;

        // record the matching incoming link on the target: target <- this
        ref LinkTableEntry incoming = ref MemoryHelpers.GetValueOrResize(ref links.IncomingLinks, targetLinkId);
        incoming.AddLink(sourceArchetype, eloc.Index);

        // events might cause structural changes, so read the flags before invoking anything
        EntityFlags sourceFlags = eloc.Flags;
        EntityFlags targetFlags = targetEloc.Flags;
        InvokeLinkEvents(world, this, sourceFlags, linkKind, EntityFlags.OnOutgoingLinked);
        InvokeLinkEvents(world, target, targetFlags, linkKind, EntityFlags.OnIncomingLinked);

        return true;
    }

    /// <summary>
    /// Assumes both this entity and the target are alive and belong to <paramref name="world"/>.
    /// </summary>
    private readonly bool UnlinkCore(LinkID linkKind, ref EntityLocation eloc, Entity target, ref EntityLocation targetEloc, World world)
    {
        if (!eloc.HasFlag(EntityFlags.HasHadOutgoingLinks))
            return false;

        ref LinkTable links = ref world.WorldLinkTable.UnsafeArrayIndex(linkKind.RawValue);

        Archetype sourceArchetype = eloc.Archetype;
        Archetype targetArchetype = targetEloc.Archetype;

        // we know link id exists since HasHadOutgoingLinks is true
        int sourceLinkId = sourceArchetype.GetExistingLinkID(eloc.Index);
        int targetLinkId = targetArchetype.GetExistingLinkID(targetEloc.Index);

        LinkTableEntry[] outgoingLinks = links.OutgoingLinks;
        if (!((uint)sourceLinkId < (uint)outgoingLinks.Length))
            return false;

        // remove the outgoing link on this entity: this -> target
        if (!outgoingLinks[sourceLinkId].RemoveLink(targetArchetype, targetEloc.Index))
            return false;

        // remove the matching incoming link on the target: target <- this
        LinkTableEntry[] incomingLinks = links.IncomingLinks;
        if ((uint)targetLinkId < (uint)incomingLinks.Length)
            incomingLinks[targetLinkId].RemoveLink(sourceArchetype, eloc.Index);

        EntityFlags sourceFlags = eloc.Flags;
        EntityFlags targetFlags = targetEloc.Flags;
        InvokeLinkEvents(world, this, sourceFlags, linkKind, EntityFlags.OnOutgoingUnlinked);
        InvokeLinkEvents(world, target, targetFlags, linkKind, EntityFlags.OnIncomingUnlinked);

        return true;
    }

    /// <summary>
    /// Invokes the world level and per entity link event identified by <paramref name="eventFlag"/> on <paramref name="entity"/>.
    /// </summary>
    private static void InvokeLinkEvents(World world, Entity entity, EntityFlags flags, LinkID linkKind, EntityFlags eventFlag)
    {
        if (!EntityLocation.HasEventFlag(flags | world.WorldEventFlags, eventFlag))
            return;

        ref EventRecord record = ref Unsafe.NullRef<EventRecord>();
        if (EntityLocation.HasEventFlag(flags, eventFlag))
            record = ref world.EventLookup.GetValueRefOrNullRef(entity.EntityIDOnly);

        bool hasRecord = !Unsafe.IsNullRef(ref record);

        switch (eventFlag)
        {
            case EntityFlags.OnOutgoingLinked:
                world.OutgoingLinkedEvent.Invoke(entity, linkKind);
                if (hasRecord)
                    record.OutgoingLinked.Invoke(entity, linkKind);
                break;
            case EntityFlags.OnIncomingLinked:
                world.IncomingLinkedEvent.Invoke(entity, linkKind);
                if (hasRecord)
                    record.IncomingLinked.Invoke(entity, linkKind);
                break;
            case EntityFlags.OnOutgoingUnlinked:
                world.OutgoingUnlinkedEvent.Invoke(entity, linkKind);
                if (hasRecord)
                    record.OutgoingUnlinked.Invoke(entity, linkKind);
                break;
            case EntityFlags.OnIncomingUnlinked:
                world.IncomingUnlinkedEvent.Invoke(entity, linkKind);
                if (hasRecord)
                    record.IncomingUnlinked.Invoke(entity, linkKind);
                break;
        }
    }

    [DoesNotReturn]
    private static void Throw_EntityIsDead() => throw new InvalidOperationException(EntityIsDeadMessage);

    //captial N null to distinguish between actual null and default
    internal string DebuggerDisplayString => IsNull ? "Null" : IsAlive ? $"World: {WorldID}, ID: {EntityID}, Version {EntityVersion}" : EntityIsDeadMessage;
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
                if (!target.IsAlive)
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


    private readonly ImmutableArray<ComponentID> AllocateComponentTypeArray()
    {
        ref EntityLocation location = ref InternalIsAlive(out _, out bool alive);
        if (!alive)
            Throw_EntityIsDead();
        if (!location.HasFlag(EntityFlags.HasHadSparseComponents))
            return location.Archetype.ArchetypeTypeArray;

        var res = ImmutableArray.CreateBuilder<ComponentID>(ArchetypicalComponentTypes.Length +
            location.GetBitset().PopCnt());

        foreach (var componentID in this)
        {
            res.Add(componentID);
        }

        return res.MoveToImmutable();
    }

    /// <summary>
    /// Enumerates the  <see cref="ComponentID"/> of all components on an entity without allocating.
    /// </summary>
    public ref struct EntityComponentIDEnumerator
    {
        /// <summary>
        /// The current <see cref="ComponentID"/> instance.
        /// </summary>
        public ComponentID Current => _current;
        private ReadOnlySpan<ComponentID> _archetypical;
        private readonly Bitset _bitset;
#if NETSTANDARD
        private ushort _currentVersion => _world.EntityTable[_entityID].Version;
        private readonly World _world;
        private int _entityID;
#else
        private readonly ref ushort _currentVersion;
#endif
        private readonly ushort _expectedVersion;
        private ComponentID _current;
        private int _index = 0;

        internal EntityComponentIDEnumerator(Entity entity)
        {
            ref EntityLocation entityLocation = ref entity.InternalIsAlive(out World world, out bool alive);
            if (!alive)
                Throw_EntityIsDead();

            _archetypical = entityLocation.ArchetypeID.Types.AsSpan();
            _bitset = entityLocation.HasFlag(EntityFlags.HasHadSparseComponents)
                ? entityLocation.GetBitset()
                : default;

            _expectedVersion = entity.EntityVersion;
#if NETSTANDARD
            _world = world;
            _entityID = entity.EntityID;
#else
            _currentVersion = ref world.EntityTable[entity.EntityID].Version;
#endif
        }

        /// <summary>
        /// Moves to the next <see cref="ComponentID"/> instance.
        /// </summary>
        /// <returns>If enumeration can continue.</returns>
        public bool MoveNext()
        {
            if (_currentVersion != _expectedVersion)
                Throw_EntityIsDead();

            if (!_archetypical.IsEmpty)
            {
                if (_index < _archetypical.Length)
                {
                    _current = _archetypical[_index++];
                    return true;
                }

                _archetypical = default;
                _index = -1;
            }

            int? found = _bitset.TryFindIndexOfBitGreaterThanOrEqualTo(_index + 1);

            if (found is int x)
            {
                _index = x;
                _current = Component.ComponentTableBySparseIndex[_index].ComponentID;
                return true;
            }

            return false;
        }
    }

    // used for fast acsess in sparse systems
    /// <summary>
    /// Also sets version.
    /// </summary>
    internal EntityLookup GetCachedLookup(World world, out Archetype archetype)
    {
        ref var record = ref world.EntityTable[EntityID];
        EntityVersion = record.Version;
        archetype = record.Archetype;
        return new EntityLookup(archetype.ComponentTagTable, archetype.Components, record.Index);
    }

    /// <summary>
    /// expected must not be default. Also sets version.
    /// </summary>
    internal EntityLookup GetCachedLookupAndAssertSparseComponent(World world, Bitset expected, out Archetype archetype)
    {
        Debug.Assert(!expected.IsDefault);
        ref var record = ref world.EntityTable[EntityID];
        EntityVersion = record.Version;
        archetype = record.Archetype;
        Span<Bitset> bitsets = archetype.SparseBitsetSpan();

        int index = record.Index;
        // match behavior of archetypical component missing
        if (!((uint)index < (uint)bitsets.Length))
            FrentExceptions.Throw_NullReferenceException();

        Bitset.AssertHasSparseComponents(ref bitsets[index], ref expected);

        return new EntityLookup(archetype.ComponentTagTable, archetype.Components, record.Index);
    }

#if NETSTANDARD
    internal ref struct EntityLookup(byte[] map, ComponentStorageRecord[] componentStorageRecord, nint index)
    {
        public ref byte MapRef => ref MemoryMarshal.GetArrayDataReference(ComponentIndexMap);

        public byte[] ComponentIndexMap = map;
        public ComponentStorageRecord[] Components = componentStorageRecord;
        public readonly nint Index = index;

        public ref T Get<T>() => ref UnsafeExtensions.UnsafeCast<T[]>(Components.UnsafeArrayIndex(ComponentIndexMap.UnsafeArrayIndex(Component<T>.ID.RawIndex) & GlobalWorldTables.IndexBits).Buffer).UnsafeArrayIndex(Index);

        public bool HasComponent<T>()
        {
            var index = ComponentIndexMap.UnsafeArrayIndex(Component<T>.ID.RawIndex) & GlobalWorldTables.IndexBits;
            return index != 0;
        }

        public bool HasTag<T>()
        {
            return (ComponentIndexMap.UnsafeArrayIndex(Core.Tag<T>.ID.RawValue) & GlobalWorldTables.HasTagMask) != 0;
        }
    }
#else
    internal ref struct EntityLookup(byte[] map, ComponentStorageRecord[] componentStorageRecord, nint index)
    {
        public ref byte MapRef => ref ComponentIndexMap;

        public ref byte ComponentIndexMap = ref MemoryMarshal.GetArrayDataReference(map);
        public ref ComponentStorageRecord Components = ref MemoryMarshal.GetArrayDataReference(componentStorageRecord);
        public readonly nint Index = index;

        public readonly ref T Get<T>()
        {
            var index = Unsafe.Add(ref ComponentIndexMap, Component<T>.ID.RawIndex) & GlobalWorldTables.IndexBits;
            T[] buffer = UnsafeExtensions.UnsafeCast<T[]>(Unsafe.Add(ref Components, index).Buffer);
            return ref buffer.UnsafeArrayIndex(Index);
        }

        public readonly bool HasComponent<T>()
        {
            var index = Unsafe.Add(ref ComponentIndexMap, Component<T>.ID.RawIndex) & GlobalWorldTables.IndexBits;
            return index != 0;
        }

        public readonly bool HasTag<T>()
        {
            return (Unsafe.Add(ref ComponentIndexMap, Core.Tag<T>.ID.RawValue) & GlobalWorldTables.HasTagMask) != 0;
        }
    }
#endif
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