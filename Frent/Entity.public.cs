using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating.Runners;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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
        AssertIsAlive(out EntityLocation entityLocation);
        return GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, componentID) < MemoryHelpers.MaxComponentCount;
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
        InternalIsAlive(out EntityLocation entityLocation) && 
        GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, componentID) < MemoryHelpers.MaxComponentCount;

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
    public ref T Get<T>()
    {
        //Total: 7x dereference

        //2x
        AssertIsAlive(out var world, out var entityLocation);

        //2x
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, Component<T>.ID);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException(typeof(T));
        //3x
        return ref ((ComponentStorage<T>)entityLocation.Archetype(world).Components[compIndex]).AsSpan()[entityLocation.ChunkIndex][entityLocation.ComponentIndex];
    }//2, 0

    /// <summary>
    /// Gets this <see cref="Entity"/>'s component of type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of component to get</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <paramref name="type"/>.</exception>
    /// <returns>The component of type <paramref name="type"/></returns>
    public object Get(Type type)
    {
        AssertIsAlive(out var world, out var entityLocation);

        //2x
        ComponentID compid = Component.GetComponentID(type);
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, compid);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
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
    public bool TryGet(Type type, [NotNullWhen(true)] out object? value)
    {
        AssertIsAlive(out var world, out var entityLocation);

        ComponentID componentId = Component.GetComponentID(type);
        int compIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, componentId);

        if (compIndex >= MemoryHelpers.MaxComponentCount)
        {
            value = null;
            return false;
        }

        value = entityLocation.Archetype(world).Components[compIndex].GetAt(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        return true;
    }
    #endregion

    #region Add
    //TODO: opt structural changes

    /// <summary>
    /// Adds a component to an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <param name="component">The component instance to add</param>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentAlreadyExistsException"><see cref="Entity"/> already has a component of type <typeparamref name="T"/></exception>
    public void Add<T>(T component)
    {
        AssertIsAlive(out var w, out var eloc);
        if (w.AllowStructualChanges)
        {
            var archetype = w.AddComponent(this, eloc, Component<T>.ID, out var to, out var location);
            ((ComponentStorage<T>)to).AsSpan()[location.ChunkIndex][location.ComponentIndex] = component;
            Core.Events.OnComponentAdded.TryInvokeAction(archetype, to, this, Component<T>.ID, location.ChunkIndex, location.ComponentIndex);
        }
        else
        {
            w.WorldUpdateCommandBuffer.AddComponent(this, component);
        }
    }

    /// <summary>
    /// Adds a boxed component as the given component ID type
    /// </summary>
    /// <param name="componentID">The ID representing the component type to add as.</param>
    /// <param name="component"></param>
    /// <remarks><paramref name="component"/> must be assignable to the type represented by <paramref name="componentID"/></remarks>
    public void Add(ComponentID componentID, object component)
    {
        AssertIsAlive(out var w, out var eloc);
        if (w.AllowStructualChanges)
        {
            var archetype = w.AddComponent(this, eloc, componentID, out var to, out var location);
            //we don't check IsAssignableTo. The reason is perf - we get InvalidCastException anyways
            to.SetAt(component, location.ChunkIndex, location.ComponentIndex);
            Core.Events.OnComponentAdded.TryInvokeAction(archetype, to, this, componentID, location.ChunkIndex, location.ComponentIndex);
        }
        else
        {
            w.WorldUpdateCommandBuffer.AddComponent(this, componentID, component);
        }
    }

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
        AssertIsAlive(out var w, out var eloc);
        if (w.AllowStructualChanges)
        {
            w.RemoveComponent(this, eloc, componentID);
        }
        else
        {
            w.WorldUpdateCommandBuffer.RemoveComponent(this, componentID);
        }
    }

    /// <summary>
    /// Removes a component from an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of component</typeparam>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <exception cref="ComponentNotFoundException"><see cref="Entity"/> does not have component of type <typeparamref name="T"/>.</exception>
    public void Remove<T>() => Remove(Component<T>.ID);

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
        AssertIsAlive(out EntityLocation entityLocation);
        return GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID);
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
    /// <typeparam name="T">The type to use as the tag.</typeparam>
    public bool Tag<T>()
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Tag(this, eloc, Core.Tag<T>.ID);
    }

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="type">The type to use as a tag</param>
    public bool Tag(Type type)
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Tag(this, eloc, Core.Tag.GetTagID(type));
    }

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <remarks>Prefer the <see cref="Tag(TagID)"/> or <see cref="Tag{T}()"/> overloads. Use <see cref="Tag{T}.ID"/> to get a <see cref="TagID"/> instance</remarks>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <param name="tagID">The tagID to use as the tag</param>
    public bool Tag(TagID tagID)
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Tag(this, eloc, tagID);
    }
    #endregion

    #region Detach
    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <typeparam name="T">The type of tag to remove.</typeparam>
    public bool Detach<T>()
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Detach(this, eloc, Core.Tag<T>.ID);
    }

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="type">The type of tag to remove.</param>
    public bool Detach(Type type)
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Detach(this, eloc, Core.Tag.GetTagID(type));
    }

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>. Tags are like components but do not take up extra memory.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    /// <returns><see langword="true"/> if the Tag was removed successfully, <see langword="false"/> when the <see cref="Entity"/> doesn't have the component</returns>
    /// <param name="tagID">The type of tag to remove.</param>
    public bool Detach(TagID tagID)
    {
        AssertIsAlive(out var w, out var eloc);
        return w.Detach(this, eloc, tagID);
    }
    #endregion

    #region Events
    public event Action<Entity, ComponentID> OnComponentAdded;
    public event Action<Entity, ComponentID> OnComponentRemoved;
    public IGenericAction<Entity> OnComponentAddedGeneric { get; set; }
    public IGenericAction<Entity> OnComponentRemovedGeneric { get; set; }

    public event Action<Entity, TagID> OnTagged
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out var location))
                return;

            Archetype archetype = location.Archetype(world);
            int mayhapsIndex = GlobalWorldTables.ComponentIndex(archetype.ID, Component<OnTagged>.ID);
            var comparr = archetype.Components;
            if (mayhapsIndex < comparr.Length)
            {
                ref var comp = ref ((ComponentStorage<OnTagged>)comparr[mayhapsIndex]).Chunks[location.ChunkIndex][location.ComponentIndex];
                comp.Tagged += value;
            }
            else
            {
                //special case???
                var t = new OnTagged();
                t.Tagged += value;
                world.WorldUpdateCommandBuffer.AddComponent(this, t);
            }
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out var location))
                return;

            Archetype archetype = location.Archetype(world);
            int mayhapsIndex = GlobalWorldTables.ComponentIndex(archetype.ID, Component<OnTagged>.ID);
            var comparr = archetype.Components;
            if (mayhapsIndex < comparr.Length)
            {
                ref var comp = ref ((ComponentStorage<OnTagged>)comparr[mayhapsIndex]).Chunks[location.ChunkIndex][location.ComponentIndex];
                comp.Tagged -= value;
            }
        }
    }
    //TODO: refactor?
    public event Action<Entity, TagID> OnDetach
    {
        add
        {
            if (value is null || !InternalIsAlive(out var world, out var location))
                return;

            Archetype archetype = location.Archetype(world);
            int mayhapsIndex = GlobalWorldTables.ComponentIndex(archetype.ID, Component<OnTagged>.ID);
            var comparr = archetype.Components;
            if (mayhapsIndex < comparr.Length)
            {
                ref var comp = ref ((ComponentStorage<OnDetached>)comparr[mayhapsIndex]).Chunks[location.ChunkIndex][location.ComponentIndex];
                comp.Detached += value;
            }
            else
            {
                var t = new OnTagged();
                t.Tagged += value;
                world.WorldUpdateCommandBuffer.AddComponent(this, t);
            }
        }
        remove
        {
            if (value is null || !InternalIsAlive(out var world, out var location))
                return;

            Archetype archetype = location.Archetype(world);
            int mayhapsIndex = GlobalWorldTables.ComponentIndex(archetype.ID, Component<OnDetached>.ID);
            var comparr = archetype.Components;
            if (mayhapsIndex < comparr.Length)
            {
                ref var comp = ref ((ComponentStorage<OnDetached>)comparr[mayhapsIndex]).Chunks[location.ChunkIndex][location.ComponentIndex];
                comp.Detached -= value;
            }
        }
    }
    #endregion

    #region Misc
    /// <summary>
    /// Deletes this entity
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public void Delete()
    {
        if (InternalIsAlive(out World? world, out EntityLocation entityLocation))
        {
            if (world.AllowStructualChanges)
            {
                world.DeleteEntity(this, entityLocation);
            }
            else
            {
                world.WorldUpdateCommandBuffer.DeleteEntity(this);
            }
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
    public bool IsAlive => InternalIsAlive();

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
        get
        {
            AssertIsAlive(out var world, out _);
            return world;
        }
    }

    /// <summary>
    /// Gets the component types for this entity, ordered in update order
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> is dead.</exception>
    public ImmutableArray<ComponentID> ComponentTypes
    {
        get
        {
            AssertIsAlive(out var world, out var loc);
            return loc.Archetype(world).ArchetypeTypeArray;
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
            AssertIsAlive(out var world, out var loc);
            return loc.Archetype(world).ArchetypeTagArray;
        }
    }

    /// <summary>
    /// The null entity
    /// </summary>
    public static Entity Null => default;
    #endregion

    #endregion
}
