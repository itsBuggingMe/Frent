﻿using Frent.Core;
using Frent.Updating;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent;

partial struct Entity
{
    #region Public API

    #region Has
    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <paramref name="componentID"/>.
    /// </summary>
    /// <param name="componentID">The component ID of the component type to check.</param>
    /// <returns><see langword="true"/> if the entity has a component of <paramref name="componentID"/>, otherwise <see langword="false"/>.</returns>
    public bool Has(ComponentID componentID)
    {
        ref EntityLookup entityLocation = ref AssertIsAlive(out _);
        return entityLocation.Location.Archetype.GetComponentIndex(componentID) != 0;
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public bool Has<T>() => Has(Component<T>.ID);

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The component type to check if this entity has.</param>
    /// <returns><see langword="true"/> if the entity has a component of <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public bool Has(Type type) => Has(Component.GetComponentID(type));

    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <paramref name="componentID"/> without throwing when dead.
    /// </summary>
    /// <param name="componentID">The component ID of the component type to check.</param>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <paramref name="componentID"/>, otherwise <see langword="false"/>.</returns>
    public bool TryHas(ComponentID componentID) =>
        InternalIsAlive(out _, out EntityLocation entityLocation) &&
        entityLocation.Archetype.GetComponentIndex(componentID) != 0;

    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <typeparamref name="T"/> without throwing when dead.
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public bool TryHas<T>() => TryHas(Component<T>.ID);
    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <paramref name="type"/> without throwing when dead.
    /// </summary>
    /// <param name="type">The type of the component type to check.</param>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public bool TryHas(Type type) => TryHas(Component.GetComponentID(type));
    #endregion

    #region Get
    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>A reference to the component in memory.</returns>
    [SkipLocalsInit]
    public ref T Get<T>()
    {
        //Total: 4x lookup

        //1x
        ref var lookup = ref AssertIsAlive(out var world);

        //1x
        //other lookup is optimized into indirect pointer addressing
        Archetype archetype = lookup.Location.Archetype;

        int compIndex = archetype.GetComponentIndex<T>();

        //2x
        //hardware trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[lookup.Location.Index];
    }//2, 0

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="type"/>.
    /// </summary>
    /// <param name="id">The ID of the type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component of type <paramref name="type"/></returns>
    public object Get(ComponentID id)
    {
        ref var lookup = ref AssertIsAlive(out var world);

        int compIndex = lookup.Location.Archetype.GetComponentIndex(id);

        return lookup.Location.Archetype.Components[compIndex].GetAt(lookup.Location.Index);
    }

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component of type <paramref name="type"/></returns>
    public object Get(Type type) => Get(Component.GetComponentID(type));

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID of the type of component to get</param>
    /// <param name="obj">The component to set</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    public void Set(ComponentID id, object obj)
    {
        ref var lookup = ref AssertIsAlive(out _);

        //2x
        int compIndex = lookup.Location.Archetype.GetComponentIndex(id);

        if (compIndex == 0)
            FrentExceptions.Throw_ComponentNotFoundException(id.Type);
        //3x
        lookup.Location.Archetype.Components[compIndex].SetAt(obj, lookup.Location.Index);
    }

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of component to get</param>
    /// <param name="obj">The component to set</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component of type <paramref name="type"/></returns>
    public void Set(Type type, object obj) => Set(Component.GetComponentID(type), obj);
    #endregion

    #region TryGet
    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if this entity has a component of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public bool TryGet<T>(out Ref<T> value)
    {
        value = TryGetCore<T>(out bool exists)!;
        return exists;
    }

    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <param name="type">The type of component to try and get</param>
    /// <returns><see langword="true"/> if this entity has a component of type <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public bool TryGet(Type type, [NotNullWhen(true)] out object? value)
    {
        ref var lookup = ref AssertIsAlive(out _);

        ComponentID componentId = Component.GetComponentID(type);
        int compIndex = GlobalWorldTables.ComponentIndex(lookup.Location.ArchetypeID, componentId);

        if (compIndex == 0)
        {
            value = null;
            return false;
        }

        value = lookup.Location.Archetype.Components[compIndex].GetAt(lookup.Location.Index);
        return true;
    }
    #endregion

    #region Add
    /// <summary>
    /// Adds a component to this <see cref="Entity"/> as its own type
    /// </summary>
    /// <param name="component">The component, which could be boxed</param>
    public void Add(object component) => Add(component.GetType(), component);

    /// <summary>
    /// Add a component to an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type to add the component as. Note that a component of type DerivedClass and BaseClass are different component types.</param>
    /// <param name="component">The component to add</param>
    public void Add(Type type, object component) => Add(Component.GetComponentID(type), component);
    #endregion

    #region Remove
    /// <summary>
    /// Removes a component from this entity
    /// </summary>
    /// <param name="componentID">The <see cref="ComponentID"/> of the component to be removed</param>
    public void Remove(ComponentID componentID)
    {
        ref var lookup = ref AssertIsAlive(out var w);
        if (w.AllowStructualChanges)
        {
            throw new NotImplementedException();
            //w.RemoveComponent(this, ref lookup, componentID);
        }
        else
        {
            w.WorldUpdateCommandBuffer.RemoveComponent(this, componentID);
        }
    }

    /// <summary>
    /// Removes a component from an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type of component to remove</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    public void Remove(Type type) => Remove(Component.GetComponentID(type));
    #endregion

    #region Tag
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
        ref var lookup = ref AssertIsAlive(out var w);
        return lookup.Location.Archetype.HasTag(tagID);
    }

    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a generic type parameter to represent the tag.
    /// </summary>
    /// <typeparam name="T">The type used as the tag.</typeparam>
    /// <returns>
    /// <see langword="true"/> if the tag of type <typeparamref name="T"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> is not alive.</exception>
    public bool Tagged<T>() => Tagged(Core.Tag<T>.ID);

    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a <see cref="Type"/> to represent the tag.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tagged(TagID)"/> or <see cref="Tagged{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <param name="type">The <see cref="Type"/> representing the tag to check.</param>
    /// <returns>
    /// <see langword="true"/> if the tag represented by <paramref name="type"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> not alive.</exception>
    public bool Tagged(Type type) => Tagged(Core.Tag.GetTagID(type));

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="type">The type to use as a tag</param>
    public bool Tag(Type type) => Tag(Core.Tag.GetTagID(type));

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tag(TagID)"/> or <see cref="Tag{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="tagID">The tagID to use as the tag</param>
    public bool Tag(TagID tagID)
    {
        ref var lookup = ref AssertIsAlive(out var w);
        if(lookup.Location.Archetype.HasTag(tagID))
            return false;

        ArchetypeID archetype = w.AddTag.FindAdjacentArchetypeID(tagID, lookup.Location.Archetype.ID, World, ArchetypeEdgeType.AddTag);
        w.MoveEntityToArchetypeIso(this, ref lookup, archetype.Archetype(w));

        return true;
    }
    #endregion

    #region Detach
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="type">The type of tag to remove.</param>
    public bool Detach(Type type) => Detach(Core.Tag.GetTagID(type));

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="tagID">The type of tag to remove.</param>
    public bool Detach(TagID tagID)
    {
        ref var lookup = ref AssertIsAlive(out var w);
        if (!lookup.Location.Archetype.HasTag(tagID))
            return false;

        ArchetypeID archetype = w.AddTag.FindAdjacentArchetypeID(tagID, lookup.Location.Archetype.ID, World, ArchetypeEdgeType.RemoveTag);
        w.MoveEntityToArchetypeIso(this, ref lookup, archetype.Archetype(w));

        return true;
    }
    #endregion

    #region Events
    /// <summary>
    /// Raised when the entity is deleted
    /// </summary>
    public event Action<Entity> OnDelete
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Delete.Push(value);
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.OnDelete;
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.OnDelete, out bool exists);
            if (exists)
            {
                events.Delete.Remove(value);
                if (!events.Add.NormalEvent.HasListeners)
                {
                    world.EntityTable[EntityID].Location.Flags &= ~EntityFlags.OnDelete;
                }
            }
        }
    }

    /// <summary>
    /// Raised when a component is added to an entity
    /// </summary>
    public event Action<Entity, ComponentID> OnComponentAdded
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Add.NormalEvent.Add(value);
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.AddComp;
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.AddComp, out bool exists);
            if (exists)
            {
                events.Add.NormalEvent.Remove(value);
                if (!events.Add.NormalEvent.HasListeners)
                {
                    world.EntityTable[EntityID].Location.Flags &= ~EntityFlags.AddComp;
                }
            }
        }
    }

    /// <summary>
    /// Raised when a component is removed from an entity
    /// </summary>
    public event Action<Entity, ComponentID> OnComponentRemoved
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Remove.NormalEvent.Add(value);
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.RemoveComp;
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.RemoveComp, out bool exists);
            if (exists)
            {
                events.Remove.NormalEvent.Remove(value);
                if (!events.Remove.NormalEvent.HasListeners)
                {
                    world.EntityTable[EntityID].Location.Flags &= ~EntityFlags.RemoveComp;
                }
            }
        }
    }

    /// <summary>
    /// Raised when a component is added to an entity, with the generic parameter
    /// </summary>
    public GenericEvent? OnComponentAddedGeneric
    {
        set
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Add.GenericEvent = value;
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.AddComp;
        }
        get
        {
            if (!InternalIsAlive(out var world, out EntityLocation entityLocation))
                return null;

            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.AddComp, out bool exists);
            if (exists)
                return events.Add.GenericEvent;
            return null;
        }
    }

    /// <summary>
    /// Raised when a component is removed to an entity, with the generic parameter
    /// </summary>
    public GenericEvent? OnComponentRemovedGeneric
    {
        set
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Remove.GenericEvent = value;
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.RemoveComp;
        }
        get
        {
            if (!InternalIsAlive(out var world, out EntityLocation entityLocation))
                return null;

            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.RemoveComp, out bool exists);
            if (exists)
                return events.Remove.GenericEvent;
            return null;
        }
    }

    /// <summary>
    /// Raised when the entity is tagged
    /// </summary>
    public event Action<Entity, TagID> OnTagged
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Tag.Add(value);
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.Tagged;
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.Tagged, out bool exists);
            if (exists)
            {
                events.Tag.Remove(value);
                if (!events.Tag.HasListeners)
                {
                    world.EntityTable[EntityID].Location.Flags &= ~EntityFlags.Tagged;
                }
            }
        }
    }

    /// <summary>
    /// Raised when a tag is detached from the entity
    /// </summary>
    public event Action<Entity, TagID> OnDetach
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
            EventRecord.Initalize(exists, ref events);
            events.Detach.Add(value);
            world.EntityTable[EntityID].Location.Flags |= EntityFlags.Detach;
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
                return;
            ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, EntityFlags.Detach, out bool exists);
            if (exists)
            {
                events.Detach.Remove(value);
                if (!events.Detach.HasListeners)
                {
                    world.EntityTable[EntityID].Location.Flags &= ~EntityFlags.Detach;
                }
            }
        }
    }
    #endregion

    #region Misc
    /// <summary>
    /// Deletes this entity
    /// </summary>
    [SkipLocalsInit]
    public void Delete()
    {
        var world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
        //hardware trap
        ref var lookup = ref world.EntityTable.UnsafeIndexNoResize(EntityID);

        if (lookup.Version != EntityVersion)
            return;

        if (world.AllowStructualChanges)
        {
            world.DeleteEntity(this, ref lookup);
        }
        else
        {
            world.WorldUpdateCommandBuffer.DeleteEntity(this);
        }
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> is still alive
    /// </summary>
    /// <returns><see langword="true"/> if this entity is still alive (not deleted), otherwise <see langword="false"/></returns>
    public bool IsAlive => InternalIsAlive(out _, out _);

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> instance is the null entity: <see langword="default"/>(<see cref="Entity"/>)
    /// </summary>
    public bool IsNull => PackedValue == 0;

    /// <summary>
    /// Gets the world this entity belongs to
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public World World
    {
        get => GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID) ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets the component types for this entity, ordered in update order
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ImmutableArray<ComponentID> ComponentTypes
    {
        get
        {
            ref var lookup = ref AssertIsAlive(out _);
            return lookup.Location.Archetype.ArchetypeTypeArray;
        }
    }

    /// <summary>
    /// Gets tags the entity has 
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ImmutableArray<TagID> TagTypes
    {
        get
        {
            ref var lookup = ref AssertIsAlive(out _);
            return lookup.Location.Archetype.ArchetypeTagArray;
        }
    }

    public EntityType Type
    {
        get
        {
            ref var lookup = ref AssertIsAlive(out _);
            return lookup.Location.Archetype.ID;
        }
    }

    /// <summary>
    /// Enumerates all components one by one
    /// </summary>
    /// <param name="onEach">The unbound generic function called on each item</param>
    public void EnumerateComponents(IGenericAction onEach)
    {
        ref var lookup = ref AssertIsAlive(out var _);
        IComponentRunner[] runners = lookup.Location.Archetype.Components;
        for(int i = 1; i < runners.Length; i++)
        {
            runners[i].InvokeGenericActionWith(onEach, lookup.Location.Index);
        }
    }

    /// <summary>
    /// The null entity
    /// </summary>
    public static Entity Null => default;

    public static EntityType EntityTypeOf(ReadOnlySpan<ComponentID> components, ReadOnlySpan<TagID> tags)
    {
        return Archetype.GetArchetypeID(components, tags);
    }
    #endregion

    #endregion
}
