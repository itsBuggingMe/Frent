using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        ref EntityLocation entityLocation = ref AssertIsAlive(out _);
        return entityLocation.Archetype.GetComponentIndex(componentID) != 0;
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
    /// <exception cref="NullReferenceException"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>A reference to the component in memory.</returns>
    [SkipLocalsInit]
    public ref T Get<T>()
    {
        //Total: 4x lookup

        //1x
        ref var lookup = ref AssertIsAlive(out var world);

        //1x
        //other lookup is optimized into indirect pointer addressing
        Archetype archetype = lookup.Archetype;

        int compIndex = archetype.GetComponentIndex<T>();

        //2x
        //hardware trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[lookup.Index];
    }//2, 0

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID of the type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="id"/>.</exception>
    /// <returns>The boxed component.</returns>
    public object Get(ComponentID id)
    {
        ref var lookup = ref AssertIsAlive(out _);

        int compIndex = lookup.Archetype.GetComponentIndex(id);

        if (compIndex == 0)
            FrentExceptions.Throw_ComponentNotFoundException(id.Type);

        return lookup.Archetype.Components[compIndex].GetAt(lookup.Index);
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
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="id"/>.</exception>
    public void Set(ComponentID id, object obj)
    {
        ref var lookup = ref AssertIsAlive(out _);

        //2x
        int compIndex = lookup.Archetype.GetComponentIndex(id);

        if (compIndex == 0)
            FrentExceptions.Throw_ComponentNotFoundException(id.Type);
        //3x
        lookup.Archetype.Components[compIndex].SetAt(this, obj, lookup.Index);
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
        value = TryGetCore<T>(out bool exists);
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
        int compIndex = GlobalWorldTables.ComponentIndex(lookup.ArchetypeID, componentId);

        if (compIndex == 0)
        {
            value = null;
            return false;
        }

        value = lookup.Archetype.Components[compIndex].GetAt(lookup.Index);
        return true;
    }
    #endregion

    #region Add
    /// <summary>
    /// Adds a range of components to an <see cref="Entity"/>, copied from component handles.
    /// </summary>
    /// <param name="componentHandles">The components to add to this entity.</param>
    /// <exception cref="ArgumentException">The additional components would exceed the archetype component limit.</exception>
    public void AddFromHandles(params ReadOnlySpan<ComponentHandle> componentHandles)
    {
        ref EntityLocation eloc = ref AssertIsAlive(out var world);
        
        if (componentHandles.Length + eloc.Archetype.ComponentTypeCount > MemoryHelpers.MaxComponentCount)
            throw new ArgumentException("Max 127 components on an entity", nameof(componentHandles));
        
        ArchetypeID finalArchetype = eloc.ArchetypeID;
        foreach (var componentHandle in componentHandles)
            finalArchetype = world.AddComponentLookup.FindAdjacentArchetypeID(componentHandle.ComponentID, finalArchetype, world, ArchetypeEdgeType.AddTag);
        
        Archetype destinationArchetype = finalArchetype.Archetype(world);
        
        world.MoveEntityToArchetypeAdd(this, ref eloc, out EntityLocation nextLocation, destinationArchetype);
        
        Span<ComponentStorageBase> buffer = MemoryHelpers.GetSharedTempComponentStorageBuffer(componentHandles.Length);

        for(int i = 0; i < componentHandles.Length; i++)
        {
            var storage = destinationArchetype.Components[destinationArchetype.GetComponentIndex(componentHandles[i].ComponentID)];
            storage.SetAt(null, componentHandles[i], nextLocation.Index);
            buffer[i] = storage;
        }

        for(int i = 0; i < componentHandles.Length; i++)
        {
            buffer[i].CallIniter(this, nextLocation.Index);
        }
    }

    /// <summary>
    /// Adds a component to this <see cref="Entity"/> as its own type
    /// </summary>
    /// <param name="component">The component, which could be boxed</param>
    public void AddBoxed(object component) => AddAs(component.GetType(), component);

    /// <summary>
    /// Add a component to an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type to add the component as. Note that a component of type DerivedClass and BaseClass are different component types.</param>
    /// <param name="component">The component to add</param>
    public void AddAs(Type type, object component) => AddAs(Component.GetComponentID(type), component);

    /// <summary>
    /// Adds a component to this <see cref="Entity"/>, as a specific component type.
    /// </summary>
    /// <param name="componentID">The component type to add as.</param>
    /// <param name="component">The component to add.</param>
    /// <exception cref="InvalidCastException"><paramref name="component"/> is not assignable to the type represented by <paramref name="componentID"/>.</exception>
    public void AddAs(ComponentID componentID, object component)
    {
        ref EntityLocation lookup = ref AssertIsAlive(out var w);
        if (w.AllowStructualChanges)
        {
            w.AddComponent(this, ref lookup, componentID, out EntityLocation entityLocation, out Archetype destination);
            
            ComponentStorageBase componentRunner = destination.Components[destination.GetComponentIndex(componentID)];
            componentRunner.SetAt(this, component, entityLocation.Index);

            if(EntityLocation.HasEventFlag(lookup.Flags | w.WorldEventFlags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
            {
                if (w.ComponentAddedEvent.HasListeners)
                    w.ComponentAddedEvent.Invoke(this, componentID);

                if (EntityLocation.HasEventFlag(lookup.Flags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
                {
#if NETSTANDARD2_1
                    EventRecord events = w.EventLookup[EntityIDOnly];
#else
                    ref EventRecord events = ref CollectionsMarshal.GetValueRefOrNullRef(w.EventLookup, EntityIDOnly);
#endif

                    events.Add.NormalEvent.Invoke(this, componentID);
                    componentRunner.InvokeGenericActionWith(events.Add.GenericEvent, this, entityLocation.Index);
                }
            }
        }
        else
        {
            w.WorldUpdateCommandBuffer.AddComponent(this, componentID, component);
        }
    }
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
            w.RemoveComponent(this, ref lookup, componentID);
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
        return lookup.Archetype.HasTag(tagID);
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
        if (lookup.Archetype.HasTag(tagID))
            return false;

        ArchetypeID archetype = w.AddTagLookup.FindAdjacentArchetypeID(tagID, lookup.Archetype.ID, World, ArchetypeEdgeType.AddTag);
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
        if (!lookup.Archetype.HasTag(tagID))
            return false;

        ArchetypeID archetype = w.AddTagLookup.FindAdjacentArchetypeID(tagID, lookup.Archetype.ID, World, ArchetypeEdgeType.RemoveTag);
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
        add => InitalizeEventRecord(value, EntityFlags.OnDelete);
        remove => UnsubscribeEvent(value, EntityFlags.OnDelete);
    }

    /// <summary>
    /// Raised when a component is added to an entity
    /// </summary>
    public event Action<Entity, ComponentID> OnComponentAdded
    {
        add => InitalizeEventRecord(value, EntityFlags.AddComp);
        remove => UnsubscribeEvent(value, EntityFlags.AddComp);
    }

    /// <summary>
    /// Raised when a component is removed from an entity
    /// </summary>
    public event Action<Entity, ComponentID> OnComponentRemoved
    {
        add => InitalizeEventRecord(value, EntityFlags.RemoveComp);
        remove => UnsubscribeEvent(value, EntityFlags.RemoveComp);
    }

    /// <summary>
    /// Raised when a component is added to an entity, with the generic parameter
    /// </summary>
    public GenericEvent? OnComponentAddedGeneric
    {
        readonly set { /*the set is just to enable the += syntax*/ }
        get
        {
            if (!InternalIsAlive(out var world, out _))
                return null;
            world.EntityTable[EntityID].Flags |= EntityFlags.AddGenericComp;
            return world.EventLookup.GetOrAddNew(EntityIDOnly).Add.GenericEvent ??= new();
        }
    }

    /// <summary>
    /// Raised when a component is removed to an entity, with the generic parameter
    /// </summary>
    public GenericEvent? OnComponentRemovedGeneric
    {
        readonly set { /*the set is just to enable the += syntax*/ }
        get
        {
            if (!InternalIsAlive(out var world, out _))
                return null;
            world.EntityTable[EntityID].Flags |= EntityFlags.RemoveGenericComp;
            return world.EventLookup.GetOrAddNew(EntityIDOnly).Remove.GenericEvent ??= new();
        }
    }

    /// <summary>
    /// Raised when the entity is tagged
    /// </summary>
    public event Action<Entity, TagID> OnTagged
    {
        add => InitalizeEventRecord(value, EntityFlags.Tagged);
        remove => UnsubscribeEvent(value, EntityFlags.Tagged);
    }

    /// <summary>
    /// Raised when a tag is detached from the entity
    /// </summary>
    public event Action<Entity, TagID> OnDetach
    {
        add => InitalizeEventRecord(value, EntityFlags.Detach);
        remove => UnsubscribeEvent(value, EntityFlags.Detach);
    }

    private void UnsubscribeEvent(object value, EntityFlags flag)
    {
        if (value is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
            return;

#if NETSTANDARD2_1
        bool exists = entityLocation.HasEvent(flag);
        var events = exists ? world.EventLookup[EntityIDOnly] : default;
#else
        ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, flag, out bool exists);
#endif


        if (exists)
        {
            bool removeFlags = false;

            switch (flag)
            {
                case EntityFlags.AddComp:
                    events!.Add.NormalEvent.Remove((Action<Entity, ComponentID>)value);
                    removeFlags = !events.Add.HasListeners;
                    break;
                case EntityFlags.RemoveComp:
                    events!.Remove.NormalEvent.Remove((Action<Entity, ComponentID>)value);
                    removeFlags = !events.Remove.HasListeners;
                    break;
                case EntityFlags.Tagged:
                    events!.Tag.Remove((Action<Entity, TagID>)value);
                    removeFlags = !events.Tag.HasListeners;
                    break;
                case EntityFlags.Detach:
                    events!.Detach.Remove((Action<Entity, TagID>)value);
                    removeFlags = !events.Detach.HasListeners;
                    break;
                case EntityFlags.OnDelete:
                    events!.Delete.Remove((Action<Entity>)value);
                    removeFlags = !events.Delete.Any;
                    break;
            }

            if (removeFlags)
                world.EntityTable[EntityID].Flags &= ~flag;
        }
    }

    private void InitalizeEventRecord(object @delegate, EntityFlags flag, bool isGenericEvent = false)
    {
        if (@delegate is null || !InternalIsAlive(out var world, out EntityLocation entityLocation))
            return;
#if NETSTANDARD2_1
        bool exists = entityLocation.HasEvent(flag);
        var record = exists ? world.EventLookup[EntityIDOnly] : default;
#else
        ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(world.EventLookup, EntityIDOnly, out bool exists);
#endif
        world.EntityTable[EntityID].Flags |= flag;
        EventRecord.Initalize(exists, ref record!);

        switch (flag)
        {
            case EntityFlags.AddComp:
                if (isGenericEvent)
                    record.Add.GenericEvent = (GenericEvent)@delegate;
                else
                    record.Add.NormalEvent.Add((Action<Entity, ComponentID>)@delegate);
                break;
            case EntityFlags.RemoveComp:
                if (isGenericEvent)
                    record.Remove.GenericEvent = (GenericEvent)@delegate;
                else
                    record.Remove.NormalEvent.Add((Action<Entity, ComponentID>)@delegate);
                break;
            case EntityFlags.Tagged:
                record.Tag.Add((Action<Entity, TagID>)@delegate);
                break;
            case EntityFlags.Detach:
                record.Detach.Add((Action<Entity, TagID>)@delegate);
                break;
            case EntityFlags.OnDelete:
                record.Delete.Push((Action<Entity>)@delegate);
                break;
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
            return lookup.Archetype.ArchetypeTypeArray;
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
            return lookup.Archetype.ArchetypeTagArray;
        }
    }

    /// <summary>
    /// The <see cref="EntityType"/> of this <see cref="Entity"/>.
    /// </summary>
    public EntityType Type
    {
        get
        {
            ref var lookup = ref AssertIsAlive(out _);
            return lookup.Archetype.ID;
        }
    }

    /// <summary>
    /// Enumerates all components one by one
    /// </summary>
    /// <param name="onEach">The unbound generic function called on each item</param>
    public void EnumerateComponents(IGenericAction onEach)
    {
        ref var lookup = ref AssertIsAlive(out var _);
        ComponentStorageBase[] runners = lookup.Archetype.Components;
        for (int i = 1; i < runners.Length; i++)
        {
            runners[i].InvokeGenericActionWith(onEach, lookup.Index);
        }
    }

    /// <summary>
    /// The null entity
    /// </summary>
    public static Entity Null => default;

    /// <summary>
    /// Gets an <see cref="EntityType"/> without needing an <see cref="Entity"/> of the specific type.
    /// </summary>
    /// <param name="components">The components the <see cref="EntityType"/> should have.</param>
    /// <param name="tags">The tags the <see cref="EntityType"/> should have.</param>
    [Obsolete("Use ArchetypeID.EntityTypeOf instead")]
    public static EntityType EntityTypeOf(ReadOnlySpan<ComponentID> components, ReadOnlySpan<TagID> tags)
    {
        return Archetype.GetArchetypeID(components, tags);
    }
    #endregion

    #endregion
}
