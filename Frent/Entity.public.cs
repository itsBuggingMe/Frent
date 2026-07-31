using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Core.Events;
using Frent.Systems;
using Frent.Updating;
using System.Collections.Immutable;
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
    public readonly bool Has(ComponentID componentID)
    {
        ref EntityLocation entityLocation = ref AssertIsAlive(out World world);
        if (componentID.IsSparseComponent)
            return world.WorldSparseSetTable.UnsafeArrayIndex(componentID.SparseIndex).Has(EntityID);
        return entityLocation.Archetype.GetComponentIndex(componentID) != 0;
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool Has<T>()
    {
        ref EntityLocation entityLocation = ref AssertIsAlive(out World world);
        if (Component<T>.IsSparseComponent)
            return world.WorldSparseSetTable.UnsafeArrayIndex(Component<T>.SparseSetComponentIndex).Has(EntityID);
        return entityLocation.Archetype.GetComponentIndex<T>() != 0;
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> has a component of Type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The component type to check if this entity has.</param>
    /// <returns><see langword="true"/> if the entity has a component of <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool Has(Type type) => Has(Component.GetComponentID(type));

    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <paramref name="componentID"/> without throwing when dead.
    /// </summary>
    /// <param name="componentID">The component ID of the component type to check.</param>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <paramref name="componentID"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHas(ComponentID componentID)
    {
        ref EntityLocation entityLocation = ref InternalIsAlive(out World world, out bool exists);
        if (exists)
        {
            if (componentID.IsSparseComponent)
            {
                return world.WorldSparseSetTable.UnsafeArrayIndex(componentID.SparseIndex).Has(EntityID);
            }
            else
            {
                return entityLocation.Archetype.GetComponentIndex(componentID) != 0;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <typeparamref name="T"/> without throwing when dead.
    /// </summary>
    /// <typeparam name="T">The type of component to check.</typeparam>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHas<T>()
    {
        ref EntityLocation entityLocation = ref InternalIsAlive(out World world, out bool exists);
        if (exists)
        {
            if (Component<T>.IsSparseComponent)
            {
                return world.WorldSparseSetTable.UnsafeArrayIndex(Component<T>.SparseSetComponentIndex).Has(EntityID);
            }
            else
            {
                return entityLocation.Archetype.GetComponentIndex(Component<T>.ID) != 0;
            }
        }

        return false;
    }
    /// <summary>
    /// Checks of this <see cref="Entity"/> has a component specified by <paramref name="type"/> without throwing when dead.
    /// </summary>
    /// <param name="type">The type of the component type to check.</param>
    /// <returns><see langword="true"/> if the entity is alive and has a component of <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHas(Type type) => TryHas(Component.GetComponentID(type));
    #endregion

    #region Get
    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="NullReferenceException"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    /// <returns>A reference to the component in memory.</returns>
    /// <remarks>
    /// The returned reference could become invalid after a structural change to a world.
    /// </remarks>
    [SkipLocalsInit]
    public readonly ref T Get<T>()
    {
        //Total: 4x lookup

        //2x
        ref var lookup = ref AssertIsAlive(out var world);

        if (Component<T>.IsSparseComponent)
        {
            var set = world.WorldSparseSetTable.UnsafeArrayIndex(Component<T>.SparseSetComponentIndex);
            return ref UnsafeExtensions.UnsafeCast<ComponentSparseSet<T>>(set).GetExisting(EntityID);
        }

        Archetype archetype = lookup.Archetype;

        int compIndex = archetype.GetComponentIndex<T>();

        //2x
        //hardware trap
        ComponentStorageRecord storage = archetype.Components.UnsafeArrayIndex(compIndex);
        return ref storage.UnsafeIndex<T>(lookup.Index);
    }//2, 0

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID of the type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="id"/>.</exception>
    /// <returns>The boxed component.</returns>
    public readonly object Get(ComponentID id)
    {
        ref var lookup = ref AssertIsAlive(out var world);

        if (id.IsSparseComponent)
        {
            var set = world.WorldSparseSetTable.UnsafeArrayIndex(id.SparseIndex);
            return set.Get(EntityID);
        }

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
    public readonly object Get(Type type) => Get(Component.GetComponentID(type));

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The ID of the type of component to get</param>
    /// <param name="obj">The component to set</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="id"/>.</exception>
    public readonly void Set(ComponentID id, object obj)
    {
        ref var lookup = ref AssertIsAlive(out World world);

        if (id.IsSparseComponent)
        {
            var set = world.WorldSparseSetTable.UnsafeArrayIndex(id.SparseIndex);
            set.Set(this, obj);
            return;
        }

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
    public readonly void Set(Type type, object obj) => Set(Component.GetComponentID(type), obj);
    #endregion

    #region TryGet
    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if this entity has a component of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// When this method returns <see langword="true"/>, <paramref name="value"/> points directly into component storage. Treat it as invalid after a
    /// structural change in the same <see cref="World"/> and retrieve the component again.
    /// </remarks>
    public readonly bool TryGet<T>(out Ref<T> value)
    {
        ref EntityLocation entityLocation = ref InternalIsAlive(out World world, out bool alive);
        if (!alive)
            goto doesntExist;

        if (Component<T>.IsSparseComponent)
        {
            value = UnsafeExtensions.UnsafeCast<ComponentSparseSet<T>>(world.WorldSparseSetTable.UnsafeArrayIndex(Component<T>.SparseSetComponentIndex))
                .TryGet(EntityID, out bool exists);
            return exists;
        }

        int compIndex = entityLocation.Archetype.GetComponentIndex<T>();

        if (compIndex == 0)
            goto doesntExist;

        T[] storage = UnsafeExtensions.UnsafeCast<T[]>(
            entityLocation.Archetype.Components.UnsafeArrayIndex(compIndex).Buffer);

        value = new Ref<T>(storage, entityLocation.Index);
        return true;

    doesntExist:
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a component from an <see cref="Entity"/>.
    /// </summary>
    /// <param name="value">A wrapper over a reference to the component when <see langword="true"/>.</param>
    /// <param name="type">The type of component to try and get</param>
    /// <returns><see langword="true"/> if this entity has a component of type <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryGet(Type type, [NotNullWhen(true)] out object? value)
    {
        ref var lookup = ref AssertIsAlive(out World world);

        ComponentID componentId = Component.GetComponentID(type);

        if (componentId.IsSparseComponent)
        {
            var set = world.WorldSparseSetTable.UnsafeArrayIndex(componentId.SparseIndex);
            return set.TryGet(EntityID, out value);
        }

        //archetype path
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
    /// Adds a set of components copied from component handles.
    /// </summary>
    /// <param name="componentHandles">The handles to copy components from.</param>
    /// <exception cref="ArgumentException">If adding <paramref name="componentHandles.Length"/> components will result in more than the maximum allowed commponent count.</exception>
    public readonly void AddFromHandles(params ReadOnlySpan<ComponentHandle> componentHandles)
    {
        AddFromHandlesCore(componentHandles, callIniters: true);
    }

    internal readonly void AddFromHandlesCore(ReadOnlySpan<ComponentHandle> componentHandles, bool callIniters)
    {
        ref EntityLocation eloc = ref AssertIsAlive(out var world);

        if(!world.AllowStructualChanges)
        {
            foreach (var handle in componentHandles)
            {
                world.WorldUpdateCommandBuffer.AddComponent(this, handle.Duplicate());
            }

            return;
        }

        if (componentHandles.Length + eloc.Archetype.ComponentTypeCount > MemoryHelpers.MaxComponentCount)
            throw new ArgumentException("Max 127 components on an entity", nameof(componentHandles));

        ArchetypeID finalArchetype = eloc.ArchetypeID;

        //TODO: setting sparse bits and calling initers.
        ref Bitset bits = ref eloc.GetBitset();

        bool moveArchetypes = false;
        foreach (var componentHandle in componentHandles)
        {
            int sparseIndex = componentHandle.ComponentID.SparseIndex;
            if (sparseIndex != 0)
            {
                world.WorldSparseSetTable.UnsafeArrayIndex(sparseIndex).Add(EntityID, componentHandle);
                eloc.Flags |= EntityFlags.HasHadSparseComponents;
                bits.Set(sparseIndex);
            }
            else
            {
                moveArchetypes = true;
                finalArchetype = world.AddComponentLookup.FindAdjacentArchetypeID(componentHandle.ComponentID, finalArchetype, world, ArchetypeEdgeType.AddComponent);
            }
        }

        Archetype destinationArchetype = finalArchetype.Archetype(world);

        EntityLocation nextLocation;
        if (moveArchetypes)
            world.MoveEntityToArchetypeAdd(this, ref eloc, out nextLocation, destinationArchetype);
        else
            nextLocation = eloc;

        Span<ComponentStorageRecord> buffer = MemoryHelpers.GetSharedTempComponentStorageBuffer(componentHandles.Length);

        // maybe cache sparse indicies on the stack
        for (int i = 0; i < componentHandles.Length; i++)
        {
            ComponentID compId = componentHandles[i].ComponentID;
            int sparseIndex = compId.SparseIndex;
            if (sparseIndex == 0)
            {
                var storage = destinationArchetype.Components[destinationArchetype.GetComponentIndex(compId)];
                storage.SetAt(null, componentHandles[i], nextLocation.Index);
                buffer[i] = storage;
            }
            else
            {
                // set above already

            }
        }

        if (callIniters)
        {
            for (int i = 0; i < componentHandles.Length; i++)
            {
                ComponentID compId = componentHandles[i].ComponentID;
                int sparseIndex = compId.SparseIndex;
                if (sparseIndex == 0)
                {
                    buffer[i].CallIniter(this, nextLocation.Index);
                }
                else
                {
                    world.WorldSparseSetTable.UnsafeArrayIndex(sparseIndex).Init(this);
                }
            }
        }


        EventRecord events = world.EventLookup.GetOrAddNew(EntityIDOnly);

        if (!events.Add.HasListeners && !world.ComponentAddedEvent.HasListeners)
            return;

        for (int i = 0; i < componentHandles.Length; i++)
        {
            ComponentID compId = componentHandles[i].ComponentID;
            int sparseIndex = compId.SparseIndex;
            events.Add.NormalEvent.Invoke(this, compId);
            world.ComponentAddedEvent.Invoke(this, compId);

            if (events.Add.GenericEvent is null)
                continue;

            if (sparseIndex == 0)
            {
                buffer[i].InvokeGenericActionWith(events.Add.GenericEvent, this, nextLocation.Index);
            }
            else
            {
                world.WorldSparseSetTable.UnsafeArrayIndex(sparseIndex).InvokeGenericEvent(this, events.Add.GenericEvent);
            }
        }
    }

    /// <summary>
    /// Adds a component to this <see cref="Entity"/> as its own type
    /// </summary>
    /// <param name="component">The component, which could be boxed</param>
    public readonly void AddBoxed(object component) => AddAs(component.GetType(), component);

    /// <summary>
    /// Add a component to an <see cref="Entity"/>
    /// </summary>
    /// <param name="type">The type to add the component as. Note that a component of type DerivedClass and BaseClass are different component types.</param>
    /// <param name="component">The component to add</param>
    public readonly void AddAs(Type type, object component) => AddAs(Component.GetComponentID(type), component);

    /// <summary>
    /// Adds a component to this <see cref="Entity"/>, as a specific component type.
    /// </summary>
    /// <param name="componentID">The component type to add as.</param>
    /// <param name="component">The component to add.</param>
    /// <exception cref="InvalidCastException"><paramref name="component"/> is not assignable to the type represented by <paramref name="componentID"/>.</exception>
    public readonly void AddAs(ComponentID componentID, object component)
    {
        ref EntityLocation lookup = ref AssertIsAlive(out var w);
        if (w.AllowStructualChanges)
        {
            ComponentStorageRecord? componentRunner = null;
            ComponentSparseSetBase? sparseSet = null;

            int entityIndex = 0;
            int sparseIndex = componentID.SparseIndex;
            if (sparseIndex != 0)
            {
                sparseSet = w.WorldSparseSetTable[sparseIndex];
                if (sparseSet.Has(EntityID))
                    FrentExceptions.Throw_ComponentAlreadyExistsException(componentID.Type);
                lookup.Flags |= EntityFlags.HasHadSparseComponents;
                lookup.GetBitset().Set(sparseIndex);
                using var tmpHandle = ComponentHandle.CreateFromBoxed(componentID, component);
                sparseSet.Add(EntityID, tmpHandle);
                sparseSet.Init(this);
            }
            else
            {
                w.AddArchetypicalComponent(this, ref lookup, componentID, out EntityLocation entityLocation, out Archetype destination);

                entityIndex = entityLocation.Index;
                componentRunner = destination.Components[destination.GetComponentIndex(componentID)];
                componentRunner.Value.SetAt(null, component, entityIndex);
                componentRunner.Value.CallIniter(this, entityIndex);
            }


            w.ComponentAddedEvent.Invoke(this, componentID);

            if (EntityLocation.HasEventFlag(lookup.Flags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
            {
                ref EventRecord events = ref w.EventLookup.GetValueRefOrNullRef(EntityIDOnly);

                events.Add.NormalEvent.Invoke(this, componentID);
                if (events.Add.GenericEvent is not null)
                {
                    sparseSet?.InvokeGenericEvent(this, events.Add.GenericEvent);
                    componentRunner?.InvokeGenericActionWith(events.Add.GenericEvent, this, entityIndex);
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
    public readonly void Remove(ComponentID componentID)
    {
        ref var lookup = ref AssertIsAlive(out var w);
        if (w.AllowStructualChanges)
        {
            w.ComponentRemovedEvent.Invoke(this, componentID);

            ref EventRecord events = ref Unsafe.NullRef<EventRecord>();
            if (EntityLocation.HasEventFlag(lookup.Flags, EntityFlags.RemoveComp | EntityFlags.RemoveGenericComp))
            {
                events = ref w.EventLookup.GetValueRefOrNullRef(EntityIDOnly);

                events.Remove.NormalEvent.Invoke(this, componentID);
            }

            int sparseIndex = componentID.SparseIndex;
            if (sparseIndex != 0)
            {
                lookup.GetBitset().ClearAt(sparseIndex);
                ComponentSparseSetBase sparseSet = w.WorldSparseSetTable.UnsafeArrayIndex(sparseIndex);

                if (!Unsafe.IsNullRef(ref events) && events.Remove.GenericEvent is { } generic)
                    sparseSet.InvokeGenericEvent(this, events.Remove.GenericEvent);

                sparseSet.Remove(EntityID, true);
            }
            else
            {
                if (!Unsafe.IsNullRef(ref events))
                    lookup.Archetype.GetComponentStorage(componentID).InvokeGenericActionWith(events.Remove.GenericEvent, this, lookup.Index);

                w.RemoveArchetypicalComponent(this, ref lookup, componentID);
            }
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
    public readonly void Remove(Type type) => Remove(Component.GetComponentID(type));
    #endregion

    #region Link
    /// <summary>
    /// Links from this <see cref="Entity"/> outgoing to <paramref name="target"/> of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of link to create.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link should point to.</param>
    /// <exception cref="InvalidOperationException">Either <see cref="Entity"/> is dead, they belong to different worlds, or the link already exists.</exception>
    public readonly void Link<T>(Entity target) => Link(Core.Link<T>.ID, target);

    /// <summary>
    /// Links from this <see cref="Entity"/> outgoing to <paramref name="target"/> of type <typeparamref name="T"/>, if it is possible to do so.
    /// </summary>
    /// <typeparam name="T">The type of link to create.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link should point to.</param>
    /// <returns><see langword="true"/> when the link was created, <see langword="false"/> when either <see cref="Entity"/> is dead, they belong to different worlds, or the link already exists.</returns>
    public readonly bool TryLink<T>(Entity target) => TryLink(Core.Link<T>.ID, target);

    /// <summary>
    /// Links from this <see cref="Entity"/> outgoing to <paramref name="target"/> with a link of kind <paramref name="linkKind"/>.
    /// </summary>
    /// <param name="linkKind">The kind of link to create.</param>
    /// <param name="target">The <see cref="Entity"/> the link should point to.</param>
    /// <exception cref="InvalidOperationException">Either <see cref="Entity"/> is dead, they belong to different worlds, or the link already exists.</exception>
    public readonly void Link(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref AssertIsAlive(out World world);
        ref EntityLocation targetEloc = ref target.AssertIsAlive(out World otherWorld);
        if (otherWorld != world)
            FrentExceptions.Throw_InvalidOperationException("This entity belongs to another world");
        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Link(this, linkKind, target);
            return;
        }
        if (!LinkCore(linkKind, ref eloc, target, ref targetEloc, world)) // TODO: improve exceptions globally
            FrentExceptions.Throw_InvalidOperationException("Link already exists");
    }

    /// <summary>
    /// Links from this <see cref="Entity"/> outgoing to <paramref name="target"/> with a link of kind <paramref name="linkKind"/>, if it is possible to do so.
    /// </summary>
    /// <param name="linkKind">The kind of link to create.</param>
    /// <param name="target">The <see cref="Entity"/> the link should point to.</param>
    /// <returns><see langword="true"/> when the link was created, <see langword="false"/> when either <see cref="Entity"/> is dead, they belong to different worlds, or the link already exists.</returns>
    public readonly bool TryLink(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref InternalIsAlive(out World world, out bool aliveThis);
        if (!aliveThis)
            return false;
        ref EntityLocation targetEloc = ref target.InternalIsAlive(out World otherWorld, out bool aliveTarget);
        if (!aliveTarget)
            return false;
        if (otherWorld != world)
            return false;
        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Link(this, linkKind, target);
            return !HasLinkCore(world, linkKind, ref eloc, ref targetEloc);
        }
        return LinkCore(linkKind, ref eloc, target, ref targetEloc, world);
    }

    /// <summary>
    /// Removes the outgoing link of type <typeparamref name="T"/> from this <see cref="Entity"/> to <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="T">The type of link to remove.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link points to.</param>
    /// <exception cref="InvalidOperationException">Either <see cref="Entity"/> is dead, they belong to different worlds, or the link does not exist.</exception>
    public readonly void Unlink<T>(Entity target) => Unlink(Core.Link<T>.ID, target);

    /// <summary>
    /// Removes the outgoing link of type <typeparamref name="T"/> from this <see cref="Entity"/> to <paramref name="target"/>, if it exists.
    /// </summary>
    /// <typeparam name="T">The type of link to remove.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link points to.</param>
    /// <returns><see langword="true"/> when the link was removed, <see langword="false"/> when either <see cref="Entity"/> is dead, they belong to different worlds, or the link does not exist.</returns>
    public readonly bool TryUnlink<T>(Entity target) => TryUnlink(Core.Link<T>.ID, target);

    /// <summary>
    /// Removes the outgoing link of kind <paramref name="linkKind"/> from this <see cref="Entity"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="linkKind">The kind of link to remove.</param>
    /// <param name="target">The <see cref="Entity"/> the link points to.</param>
    /// <exception cref="InvalidOperationException">Either <see cref="Entity"/> is dead, they belong to different worlds, or the link does not exist.</exception>
    public readonly void Unlink(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref AssertIsAlive(out World world);
        ref EntityLocation targetEloc = ref target.AssertIsAlive(out World otherWorld);
        if (otherWorld != world)
            FrentExceptions.Throw_InvalidOperationException("This entity belongs to another world");
        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Unlink(this, linkKind, target);
            return;
        }
        if (!UnlinkCore(linkKind, ref eloc, target, ref targetEloc, world)) // TODO: improve exceptions globally
            FrentExceptions.Throw_InvalidOperationException("Link does not exist");
    }

    /// <summary>
    /// Removes the outgoing link of kind <paramref name="linkKind"/> from this <see cref="Entity"/> to <paramref name="target"/>, if it exists.
    /// </summary>
    /// <param name="linkKind">The kind of link to remove.</param>
    /// <param name="target">The <see cref="Entity"/> the link points to.</param>
    /// <returns><see langword="true"/> when the link was removed, <see langword="false"/> when either <see cref="Entity"/> is dead, they belong to different worlds, or the link does not exist.</returns>
    public readonly bool TryUnlink(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref InternalIsAlive(out World world, out bool aliveThis);
        if (!aliveThis)
            return false;
        ref EntityLocation targetEloc = ref target.InternalIsAlive(out World otherWorld, out bool aliveTarget);
        if (!aliveTarget)
            return false;
        if (otherWorld != world)
            return false;
        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Unlink(this, linkKind, target);
            return HasLinkCore(world, linkKind, ref eloc, ref targetEloc);
        }
        return UnlinkCore(linkKind, ref eloc, target, ref targetEloc, world);
    }

    /// <summary>
    /// Checks whether this <see cref="Entity"/> links to anything with a link of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <returns><see langword="true"/> when this <see cref="Entity"/> is the source of at least one link of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This <see cref="Entity"/> is dead.</exception>
    public readonly bool HasOutgoingLink<T>() => HasOutgoingLink(Core.Link<T>.ID);

    /// <summary>
    /// Checks whether anything links to this <see cref="Entity"/> with a link of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <returns><see langword="true"/> when this <see cref="Entity"/> is the target of at least one link of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This <see cref="Entity"/> is dead.</exception>
    public readonly bool HasIncomingLink<T>() => HasIncomingLink(Core.Link<T>.ID);

    /// <inheritdoc cref="HasOutgoingLink{T}()"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    public readonly bool HasOutgoingLink(LinkID linkKind) => HasLinkGeneral(linkKind, 0);

    /// <inheritdoc cref="HasIncomingLink{T}()"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    public readonly bool HasIncomingLink(LinkID linkKind) => HasLinkGeneral(linkKind, 1);

    /// <summary>
    /// Checks whether this <see cref="Entity"/> links to <paramref name="target"/> with a link of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// There is no incoming counterpart taking an <see cref="Entity"/>, since links are stored in both directions:
    /// ask whether <c>source</c> links to this <see cref="Entity"/> with <c>source.HasOutgoingLink&lt;T&gt;(this)</c>.
    /// </remarks>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link would point to.</param>
    /// <returns><see langword="true"/> when a link of type <typeparamref name="T"/> goes from this <see cref="Entity"/> to <paramref name="target"/>, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Either <see cref="Entity"/> is dead.</exception>
    /// <exception cref="ArgumentException">The entities belong to different worlds.</exception>
    public readonly bool HasOutgoingLink<T>(Entity target) => HasOutgoingLink(Core.Link<T>.ID, target);

    /// <inheritdoc cref="HasOutgoingLink{T}(Entity)"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    /// <param name="target">The <see cref="Entity"/> the link would point to.</param>
    public readonly bool HasOutgoingLink(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref AssertIsAlive(out World world);
        ref EntityLocation targetEloc = ref target.AssertIsAlive(out World otherWorld);
        if (otherWorld != world)
            FrentExceptions.Throw_ArgumentException("Target must be from the same world!");
        return HasLinkCore(world, linkKind, ref eloc, ref targetEloc);
    }

    /// <summary>
    /// Checks whether this <see cref="Entity"/> links to anything with a link of type <typeparamref name="T"/>, without throwing when dead.
    /// </summary>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <returns><see langword="true"/> when this <see cref="Entity"/> is alive and is the source of at least one link of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHasOutgoingLink<T>() => TryHasOutgoingLink(Core.Link<T>.ID);

    /// <summary>
    /// Checks whether anything links to this <see cref="Entity"/> with a link of type <typeparamref name="T"/>, without throwing when dead.
    /// </summary>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <returns><see langword="true"/> when this <see cref="Entity"/> is alive and is the target of at least one link of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHasIncomingLink<T>() => TryHasIncomingLink(Core.Link<T>.ID);

    /// <inheritdoc cref="TryHasOutgoingLink{T}()"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    public readonly bool TryHasOutgoingLink(LinkID linkKind) => TryHasLinkGeneral(linkKind, 0);

    /// <inheritdoc cref="TryHasIncomingLink{T}()"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    public readonly bool TryHasIncomingLink(LinkID linkKind) => TryHasLinkGeneral(linkKind, 1);

    /// <summary>
    /// Checks whether this <see cref="Entity"/> links to <paramref name="target"/> with a link of type <typeparamref name="T"/>, without throwing when dead.
    /// </summary>
    /// <inheritdoc cref="HasOutgoingLink{T}(Entity)" path="/remarks"/>
    /// <typeparam name="T">The type of link to check for.</typeparam>
    /// <param name="target">The <see cref="Entity"/> the link would point to.</param>
    /// <returns><see langword="true"/> when both entities are alive and a link of type <typeparamref name="T"/> goes from this <see cref="Entity"/> to <paramref name="target"/>, otherwise <see langword="false"/>.</returns>
    public readonly bool TryHasOutgoingLink<T>(Entity target) => TryHasOutgoingLink(Core.Link<T>.ID, target);

    /// <inheritdoc cref="TryHasOutgoingLink{T}(Entity)"/>
    /// <param name="linkKind">The kind of link to check for.</param>
    /// <param name="target">The <see cref="Entity"/> the link would point to.</param>
    public readonly bool TryHasOutgoingLink(LinkID linkKind, Entity target)
    {
        ref EntityLocation eloc = ref InternalIsAlive(out World world, out bool aliveThis);
        if (!aliveThis)
            return false;
        ref EntityLocation targetEloc = ref target.InternalIsAlive(out World otherWorld, out bool aliveTarget);
        if (!aliveTarget)
            return false;
        if (otherWorld != world)
            return false;
        return HasLinkCore(world, linkKind, ref eloc, ref targetEloc);
    }

    /// <summary>
    /// Enumerates every <see cref="Entity"/> that links to this one with a link of type <typeparamref name="TLink"/>.
    /// </summary>
    /// <typeparam name="TLink">The type of link to enumerate.</typeparam>
    /// <exception cref="InvalidOperationException">This <see cref="Entity"/> is dead.</exception>
    public readonly EntityLinkEnumerator.Enumerable EnumerateIncomingWithEntities<TLink>() => EnumerateIncomingWithEntities(Core.Link<TLink>.ID);

    /// <summary>
    /// Enumerates every <see cref="Entity"/> this one links to with a link of type <typeparamref name="TLink"/>.
    /// </summary>
    /// <inheritdoc cref="EnumerateIncomingWithEntities{TLink}()"/>
    public readonly EntityLinkEnumerator.Enumerable EnumerateOutgoingWithEntities<TLink>() => EnumerateOutgoingWithEntities(Core.Link<TLink>.ID);

    /// <inheritdoc cref="EnumerateIncomingWithEntities{TLink}()"/>
    /// <param name="linkID">The kind of link to enumerate.</param>
    public readonly EntityLinkEnumerator.Enumerable EnumerateIncomingWithEntities(LinkID linkID) => new EntityLinkEnumerator.Enumerable(this, linkID, 1);

    /// <inheritdoc cref="EnumerateOutgoingWithEntities{TLink}()"/>
    /// <param name="linkID">The kind of link to enumerate.</param>
    public readonly EntityLinkEnumerator.Enumerable EnumerateOutgoingWithEntities(LinkID linkID) => new EntityLinkEnumerator.Enumerable(this, linkID, 0);
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
    public readonly bool Tagged(TagID tagID)
    {
        ref var lookup = ref AssertIsAlive(out _);
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
    public readonly bool Tagged<T>() => Tagged(Core.Tag<T>.ID);

    /// <summary>
    /// Checks whether this <see cref="Entity"/> has a specific tag, using a <see cref="Type"/> to represent the tag.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tagged(TagID)"/> or <see cref="Tagged{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <param name="type">The <see cref="Type"/> representing the tag to check.</param>
    /// <returns>
    /// <see langword="true"/> if the tag represented by <paramref name="type"/> has this <see cref="Entity"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Entity"/> not alive.</exception>
    public readonly bool Tagged(Type type) => Tagged(Core.Tag.GetTagID(type));

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="type">The type to use as a tag</param>
    public readonly bool Tag(Type type) => Tag(Core.Tag.GetTagID(type));

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tag(TagID)"/> or <see cref="Tag{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="tagID">The tagID to use as the tag</param>
    public readonly bool Tag(TagID tagID)
    {
        ref var lookup = ref AssertIsAlive(out var w);

        if (lookup.Archetype.HasTag(tagID))
            return false;

        if (!w.AllowStructualChanges)
        {
            w.WorldUpdateCommandBuffer.Tag(this, tagID);
            return !lookup.Archetype.HasTag(tagID);
        }

        ArchetypeID archetype = w.AddTagLookup.FindAdjacentArchetypeID(tagID, lookup.Archetype.ID, World, ArchetypeEdgeType.AddTag);
        w.MoveEntityToArchetypeIso(this, ref lookup, archetype.Archetype(w));

        EntityFlags flags = lookup.Flags | w.WorldEventFlags;
        if (EntityLocation.HasEventFlag(flags, EntityFlags.Tagged))
        {
            if (w.Tagged.HasListeners)
                w.Tagged.Invoke(this, tagID);

            ref EventRecord events = ref w.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
            if (!Unsafe.IsNullRef(ref events))
                events.Tag.Invoke(this, tagID);
        }

        return true;
    }

    /// <summary>
    /// Adds a set of tags to this entity.
    /// </summary>
    /// <param name="tagIds">The tag types to add.</param>
    /// <remarks>You can get a <see cref="TagID"/> by using <see cref="TagTypes" /> or <see cref="Tag.GetTagID(System.Type)"/></remarks>
    public readonly void TagFromIDs(ReadOnlySpan<TagID> tagIds)
    {
        ref var lookup = ref AssertIsAlive(out var w);

        if (tagIds.Length == 0)
            return;

        if (!w.AllowStructualChanges)
        {
            foreach(var tagId in tagIds)
                w.WorldUpdateCommandBuffer.Tag(this, tagId);
            return;
        }

        ImmutableArray<TagID> pTags = lookup.ArchetypeID.Tags;
        ImmutableArray<ComponentID> components = lookup.ArchetypeID.Types;
        Span<TagID> newIds = stackalloc TagID[tagIds.Length + pTags.Length];

        tagIds.CopyTo(newIds);
        pTags.AsSpan().CopyTo(newIds[tagIds.Length..]);

        if (MemoryHelpers.HasDuplicateIDs(newIds, out TagID duplicate))
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a tag of type {duplicate.Type.Name}");

        Archetype dest = Archetype.CreateOrGetExistingArchetype(components.AsSpan(), newIds, w, components);

        w.MoveEntityToArchetypeIso(this, ref lookup, dest);

        EntityFlags flags = lookup.Flags | w.WorldEventFlags;
        if (EntityLocation.HasEventFlag(flags, EntityFlags.Tagged))
        {
            if (w.Tagged.HasListeners)
            {
                foreach(var tag in tagIds)
                    w.Tagged.Invoke(this, tag);
            }


            if (EntityLocation.HasEventFlag(lookup.Flags, EntityFlags.Tagged))
            {
                var @event = w.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                foreach(var tag in tagIds)
                    @event.Tag.Invoke(this, tag);
            }
        }
    }
    #endregion

    #region Detach
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="type">The type of tag to remove.</param>
    public readonly bool Detach(Type type) => Detach(Core.Tag.GetTagID(type));

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="tagID">The type of tag to remove.</param>
    public readonly bool Detach(TagID tagID)
    {
        ref var lookup = ref AssertIsAlive(out var world);
        if (!lookup.Archetype.HasTag(tagID))
            return false;

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Detach(this, tagID);
            return true;
        }

        ArchetypeID archetype = world.RemoveTagLookup.FindAdjacentArchetypeID(tagID, lookup.Archetype.ID, World, ArchetypeEdgeType.RemoveTag);
        world.MoveEntityToArchetypeIso(this, ref lookup, archetype.Archetype(world));

        EntityFlags flags = lookup.Flags | world.WorldEventFlags;
        if (EntityLocation.HasEventFlag(flags, EntityFlags.Detach))
        {
            world.Detached.Invoke(this, tagID);

            if (EntityLocation.HasEventFlag(flags, EntityFlags.Detach))
            {
                ref EventRecord events = ref world.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                if (EntityLocation.HasEventFlag(lookup.Flags, EntityFlags.Detach))
                {
                    events.Detach.Invoke(this, tagID);
                }
            }
        }

        return true;
    }
    #endregion

    #region Events
    /// <summary>
    /// Raised when the entity is deleted
    /// </summary>
    public readonly event Action<Entity> OnDelete
    {
        add => InitalizeEventRecord(value, EntityFlags.OnDelete);
        remove => UnsubscribeEvent(value, EntityFlags.OnDelete);
    }

    /// <summary>
    /// Raised when a component is added to an entity
    /// </summary>
    public readonly event Action<Entity, ComponentID> OnComponentAdded
    {
        add => InitalizeEventRecord(value, EntityFlags.AddComp);
        remove => UnsubscribeEvent(value, EntityFlags.AddComp);
    }

    /// <summary>
    /// Raised when a component is removed from an entity
    /// </summary>
    public readonly event Action<Entity, ComponentID> OnComponentRemoved
    {
        add => InitalizeEventRecord(value, EntityFlags.RemoveComp);
        remove => UnsubscribeEvent(value, EntityFlags.RemoveComp);
    }

    /// <summary>
    /// Raised when a component is added to an entity, with the generic parameter
    /// </summary>
    public readonly GenericEvent? OnComponentAddedGeneric
    {
        set { /*the set is just to enable the += syntax*/ }
        get
        {
            InternalIsAlive(out World world, out bool alive);
            if (!alive)
                return null;
            world.EntityTable[EntityID].Flags |= EntityFlags.AddGenericComp;
            return world.EventLookup.GetOrAddNew(EntityIDOnly).Add.GenericEvent ??= new();
        }
    }

    /// <summary>
    /// Raised when a component is removed to an entity, with the generic parameter
    /// </summary>
    public readonly GenericEvent? OnComponentRemovedGeneric
    {
        set { /*the set is just to enable the += syntax*/ }
        get
        {
            InternalIsAlive(out World world, out bool alive);
            if (!alive)
                return null;
            world.EntityTable[EntityID].Flags |= EntityFlags.RemoveGenericComp;
            return world.EventLookup.GetOrAddNew(EntityIDOnly).Remove.GenericEvent ??= new();
        }
    }

    /// <summary>
    /// Raised when the entity is tagged
    /// </summary>
    public readonly event Action<Entity, TagID> OnTagged
    {
        add => InitalizeEventRecord(value, EntityFlags.Tagged);
        remove => UnsubscribeEvent(value, EntityFlags.Tagged);
    }

    /// <summary>
    /// Raised when a tag is detached from the entity
    /// </summary>
    public readonly event Action<Entity, TagID> OnDetach
    {
        add => InitalizeEventRecord(value, EntityFlags.Detach);
        remove => UnsubscribeEvent(value, EntityFlags.Detach);
    }

    /// <summary>
    /// Raised when this entity becomes the target of a link
    /// </summary>
    public readonly event Action<Entity, LinkID> OnIncomingLinked
    {
        add => InitalizeEventRecord(value, EntityFlags.OnIncomingLinked);
        remove => UnsubscribeEvent(value, EntityFlags.OnIncomingLinked);
    }

    /// <summary>
    /// Raised when this entity becomes the source of a link
    /// </summary>
    public readonly event Action<Entity, LinkID> OnOutgoingLinked
    {
        add => InitalizeEventRecord(value, EntityFlags.OnOutgoingLinked);
        remove => UnsubscribeEvent(value, EntityFlags.OnOutgoingLinked);
    }

    /// <summary>
    /// Raised when an incoming link to this entity is removed
    /// </summary>
    public readonly event Action<Entity, LinkID> OnIncomingUnlinked
    {
        add => InitalizeEventRecord(value, EntityFlags.OnIncomingUnlinked);
        remove => UnsubscribeEvent(value, EntityFlags.OnIncomingUnlinked);
    }

    /// <summary>
    /// Raised when an outgoing link from this entity is removed
    /// </summary>
    public readonly event Action<Entity, LinkID> OnOutgoingUnlinked
    {
        add => InitalizeEventRecord(value, EntityFlags.OnOutgoingUnlinked);
        remove => UnsubscribeEvent(value, EntityFlags.OnOutgoingUnlinked);
    }

    private readonly void UnsubscribeEvent(object value, EntityFlags flag)
    {
        if (value is null)
            return;

        ref EntityLocation entityLocation = ref InternalIsAlive(out World world, out bool alive);
        if (!alive)
            return;

        ref var events = ref world.TryGetEventData(entityLocation, EntityIDOnly, flag, out bool exists);

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
                case EntityFlags.OnIncomingLinked:
                    events!.IncomingLinked.Remove((Action<Entity, LinkID>)value);
                    removeFlags = !events.IncomingLinked.HasListeners;
                    break;
                case EntityFlags.OnOutgoingLinked:
                    events!.OutgoingLinked.Remove((Action<Entity, LinkID>)value);
                    removeFlags = !events.OutgoingLinked.HasListeners;
                    break;
                case EntityFlags.OnIncomingUnlinked:
                    events!.IncomingUnlinked.Remove((Action<Entity, LinkID>)value);
                    removeFlags = !events.IncomingUnlinked.HasListeners;
                    break;
                case EntityFlags.OnOutgoingUnlinked:
                    events!.OutgoingUnlinked.Remove((Action<Entity, LinkID>)value);
                    removeFlags = !events.OutgoingUnlinked.HasListeners;
                    break;
            }

            if (removeFlags)
                world.EntityTable[EntityID].Flags &= ~flag;
        }
    }

    private readonly void InitalizeEventRecord(object @delegate, EntityFlags flag, bool isGenericEvent = false)
    {
        if (@delegate is null)
            return;

        InternalIsAlive(out World world, out bool alive);
        if (!alive)
            return;

        ref var record = ref world.EventLookup.GetValueRefOrAddDefault(EntityIDOnly, out bool exists);

        world.EntityTable[EntityID].Flags |= flag;
        record ??= new();

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
            case EntityFlags.OnIncomingLinked:
                record.IncomingLinked.Add((Action<Entity, LinkID>)@delegate);
                break;
            case EntityFlags.OnOutgoingLinked:
                record.OutgoingLinked.Add((Action<Entity, LinkID>)@delegate);
                break;
            case EntityFlags.OnIncomingUnlinked:
                record.IncomingUnlinked.Add((Action<Entity, LinkID>)@delegate);
                break;
            case EntityFlags.OnOutgoingUnlinked:
                record.OutgoingUnlinked.Add((Action<Entity, LinkID>)@delegate);
                break;
        }
    }

    #endregion 

    #region Misc
    /// <summary>
    /// Deletes this entity
    /// </summary>
    [SkipLocalsInit]
    public readonly void Delete()
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
    public readonly bool IsAlive
    {
        get
        {
            InternalIsAlive(out _, out bool alive);
            return alive;
        }
    }

    /// <summary>
    /// Checks to see if this <see cref="Entity"/> instance is the null entity: <see langword="default"/>(<see cref="Entity"/>)
    /// </summary>
    public readonly bool IsNull => PackedValue == 0;

    /// <summary>
    /// Gets the world this entity belongs to
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public readonly World World
    {
        get
        {
            World? world = GlobalWorldTables.Worlds.UnsafeIndexNoResize(WorldID);
            if (world is null)
                Throw_EntityIsDead();
            return world;
        }
    }

    /// <summary>
    /// Gets all the component types for this entity.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public readonly ImmutableArray<ComponentID> ComponentTypes => AllocateComponentTypeArray();

    /// <summary>
    /// Gets the archetypical component types for this entity.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public readonly ImmutableArray<ComponentID> ArchetypicalComponentTypes
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
    public readonly ImmutableArray<TagID> TagTypes
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
    public readonly EntityType Type
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
    public readonly void EnumerateComponents(IGenericAction onEach)
    {
        ref var lookup = ref AssertIsAlive(out var world);
        ComponentStorageRecord[] runners = lookup.Archetype.Components;
        for (int i = 1; i < runners.Length; i++)
        {
            runners[i].InvokeGenericActionWith(onEach, lookup.Index);
        }

        if (!lookup.HasFlag(EntityFlags.HasHadSparseComponents))
            return;

        ref Bitset bitset = ref lookup.GetBitset();
        foreach (int sparseComponentId in bitset)
        {
            world.WorldSparseSetTable.UnsafeArrayIndex(sparseComponentId)
                .InvokeGenericActionWith(onEach, EntityID);
        }
    }

    /// <summary>
    /// Gets a <see cref="EntityComponentIDEnumerator"/> that can be used to enumerate all component types on this entity.
    /// </summary>
    /// <remarks>Can be used to enumerate sparse and archetypical component types without allocating.</remarks>
    public readonly EntityComponentIDEnumerator GetEnumerator() => new(this);

    /// <summary>
    /// The null entity
    /// </summary>
    public static Entity Null => default;
    #endregion

    #endregion
}
