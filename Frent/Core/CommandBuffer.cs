using Frent.Collections;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

/// <summary>
/// Stores a set of structual changes that can be applied to a <see cref="World"/>.
/// </summary>
public class CommandBuffer
{
    internal FastStack<AddComponent> _addComponentBuffer = FastStack<AddComponent>.Create(2);
    internal FastStack<DeleteComponent> _removeComponentBuffer = FastStack<DeleteComponent>.Create(2);
    internal FastStack<TagCommand> _tagEntityBuffer = FastStack<TagCommand>.Create(2);
    internal FastStack<TagCommand> _detachTagEntityBuffer = FastStack<TagCommand>.Create(2);

    internal FastStack<CreateCommand> _createEntityBuffer = FastStack<CreateCommand>.Create(2);
    internal FastStack<ComponentHandle> _createEntityComponents = FastStack<ComponentHandle>.Create(2);
    private readonly ComponentStorageRecord[] _componentRunnerBuffer = new ComponentStorageRecord[MemoryHelpers.MaxComponentCount];
    internal FastStack<EntityIDOnly> _deleteEntityBuffer = FastStack<EntityIDOnly>.Create(2);

    internal World _world;
    //-1 indicates normal state
    internal int _lastCreateEntityComponentsBufferIndex = -1;
    internal bool _isInactive;

    /// <summary>
    /// Whether or not the buffer currently has items to be played back.
    /// </summary>
    public bool HasBufferItems => !_isInactive;

    /// <summary>
    /// Creates a command buffer, which stores changes to a world without directly applying them.
    /// </summary>
    /// <param name="world">The world to apply things to.</param>
    public CommandBuffer(World world)
    {
        _world = world;
        _isInactive = true;
    }

    /// <summary>
    /// Deletes a component from when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity that will be deleted on playback.</param>
    public void DeleteEntity(Entity entity)
    {
        SetIsActive();
        _deleteEntityBuffer.Push(entity.EntityIDOnly);
    }

    /// <summary>
    /// Removes a component from when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to remove a component from.</param>
    /// <param name="component">The component to remove.</param>
    public void RemoveComponent(Entity entity, ComponentID component)
    {
        SetIsActive();
        _removeComponentBuffer.Push(new DeleteComponent(entity.EntityIDOnly, component));
    }

    /// <summary>
    /// Removes a component from when <see cref="Playback"/> is called.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove a component from.</param>
    public void RemoveComponent<T>(Entity entity) => RemoveComponent(entity, Component<T>.ID);

    /// <summary>
    /// Removes a component from when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to remove a component from.</param>
    /// <param name="type">The type of component to remove.</param>
    public void RemoveComponent(Entity entity, Type type) => RemoveComponent(entity, Component.GetComponentID(type));

    /// <summary>
    /// Adds a component to an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add to.</param>
    /// <param name="component">The component to add.</param>
    public void AddComponent<T>(Entity entity, in T component)
    {
        SetIsActive();
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, ComponentHandle.Create(component)));
    }

    /// <summary>
    /// Adds a component to an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to add to.</param>
    /// <param name="component">The component to add.</param>
    /// <param name="componentID">The ID of the component type to add as.</param>
    /// <remarks><paramref name="component"/> must be assignable to <see cref="ComponentID.Type"/>.</remarks>
    public void AddComponent(Entity entity, ComponentID componentID, object component)
    {
        SetIsActive();
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, ComponentHandle.CreateFromBoxed(componentID, component)));
    }

    /// <summary>
    /// Adds a component to an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to add to.</param>
    /// <param name="component">The component to add.</param>
    /// <param name="componentType">The type to add the component as.</param>
    /// <remarks><paramref name="component"/> must be assignable to <paramref name="componentType"/>.</remarks>
    public void AddComponent(Entity entity, Type componentType, object component) => AddComponent(entity, Component.GetComponentID(componentType), component);

    /// <summary>
    /// Adds a component to an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to add to.</param>
    /// <param name="component">The component to add.</param>
    public void AddComponent(Entity entity, object component) => AddComponent(entity, component.GetType(), component);

    #region Tags
    /// <summary>
    /// Tags an entity with a tag when <see cref="Playback"/> is called.
    /// </summary>
    /// <typeparam name="T">The type to tag the entity with.</typeparam>
    /// <param name="entity">The entity to tag.</param>

    public void Tag<T>(Entity entity) => Tag(entity, Core.Tag<T>.ID);
    /// <summary>
    /// Tags an entity with a tag when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="tagID">The ID of the tag type to tag.</param>
    /// <param name="entity">The entity to tag.</param>
    public void Tag(Entity entity, TagID tagID)
    {
        SetIsActive();
        _tagEntityBuffer.Push(new(entity.EntityIDOnly, tagID));
    }

    /// <summary>
    /// Detaches a tag from an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <typeparam name="T">The type of tag to detach.</typeparam>
    /// <param name="entity">The entity to detach from.</param>
    public void Detach<T>(Entity entity) => Detach(entity, Core.Tag<T>.ID);
    /// <summary>
    /// Detaches a tag from an entity when <see cref="Playback"/> is called.
    /// </summary>
    /// <param name="entity">The entity to detach from.</param>
    /// <param name="tagID">The ID of the tag type to detach from the entity.</param>
    public void Detach(Entity entity, TagID tagID)
    {
        SetIsActive();
        _detachTagEntityBuffer.Push(new(entity.EntityIDOnly, tagID));
    }
    #endregion

    #region Create
    /// <summary>
    /// Begins to create an entity, which will be resolved when <see cref="Playback"/> is called.
    /// </summary>
    /// <returns><see langword="this"/> instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException">An entity is already being created.</exception>
    public CommandBuffer Entity()
    {
        SetIsActive();
        if (_lastCreateEntityComponentsBufferIndex >= 0)
        {
            throw new InvalidOperationException("An entity is currently being created! Use 'End' to finish an entity creation!");
        }
        _lastCreateEntityComponentsBufferIndex = _createEntityComponents.Count;
        return this;
    }

    /// <summary>
    /// Records <paramref name="component"/> to be part of the entity created when resolved.
    /// </summary>
    /// <returns><see langword="this"/> instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> has not been called."/></exception>
    public CommandBuffer With<T>(T component)
    {
        AssertCreatingEntity();
        _createEntityComponents.Push(ComponentHandle.Create(in component));
        return this;
    }

    /// <summary>
    /// Records <paramref name="component"/> to be part of the entity created when resolved as a component type represented by <paramref name="componentID"/>.
    /// </summary>
    /// <returns><see langword="this"/> instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> has not been called."/></exception>
    public CommandBuffer WithBoxed(ComponentID componentID, object component)
    {
        AssertCreatingEntity();
        //we don't check IsAssignableTo - reason is perf - InvalidCastException anyways
        int index = Component.ComponentTable[componentID.RawIndex].Storage.CreateBoxed(component);
        _createEntityComponents.Push(new ComponentHandle(index, componentID));
        return this;
    }

    /// <summary>
    /// Records <paramref name="component"/> to be part of the entity created when resolved.
    /// </summary>
    /// <returns><see langword="this"/> instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> has not been called."/></exception>
    public CommandBuffer WithBoxed(object component) => WithBoxed(component.GetType(), component);

    /// <summary>
    /// Records <paramref name="component"/> to be part of the entity created when resolved as a component type of <paramref name="type"/>.
    /// </summary>
    /// <returns><see langword="this"/> instance, for method chaining.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Entity"/> has not been called."/></exception>
    public CommandBuffer WithBoxed(Type type, object component) => WithBoxed(Component.GetComponentID(type), component);

    /// <summary>
    /// Finishes recording entity creation and returns an entity with zero components. Recorded components will be added on playback.
    /// </summary>
    /// <returns>The created entity ID</returns>
    public Entity End()
    {
        //CreateCommand points to a segment of the _createEntityComponents stack
        var e = _world.CreateEntityWithoutEvent();
        _createEntityBuffer.Push(new CreateCommand(
            e.EntityIDOnly,
            _lastCreateEntityComponentsBufferIndex,
            _createEntityComponents.Count - _lastCreateEntityComponentsBufferIndex));
        _lastCreateEntityComponentsBufferIndex = -1;
        return e;
    }
    #endregion

    /// <summary>
    /// Removes all commands without playing them back.
    /// </summary>
    /// <remarks>This command also removes all empty entities (without events) that have been created by this command buffer.</remarks>
    public void Clear()
    {
        _isInactive = true;

        while (_createEntityBuffer.TryPop(out CreateCommand createCommand))
        {
            var item = createCommand.Entity;
            ref var record = ref _world.EntityTable[item.ID];
            if (record.Version == item.Version)
            {
                _world.DeleteEntityWithoutEvents(item.ToEntity(_world), ref record);
            }
        }

        _removeComponentBuffer.Clear();
        _deleteEntityBuffer.Clear();
    }

    /// <summary>
    /// Plays all the queued commands, applying them to a world.
    /// </summary>
    /// <returns><see langword="true"/> when at least one change was made; <see langword="false"/> when this command buffer is empty and not active.</returns>
    public bool Playback()
    {
        if (!_world.AllowStructualChanges)
            FrentExceptions.Throw_InvalidOperationException("The world currently does not allow structural changes!");
        return PlaybackInternal();
    }

    internal bool PlaybackInternal()
    {
        bool hasItems = 
            _createEntityBuffer.Count > 0 ||
            _deleteEntityBuffer.Count > 0 ||
            _addComponentBuffer.Count > 0 ||
            _removeComponentBuffer.Count > 0 ||
            _tagEntityBuffer.Count > 0 ||
            _detachTagEntityBuffer.Count > 0
            ;

        if (!hasItems)
            return hasItems;

        while (_createEntityBuffer.TryPop(out CreateCommand createCommand))
        {
            Entity concrete = createCommand.Entity.ToEntity(_world);
            ref EntityLocation lookup = ref _world.EntityTable.UnsafeIndexNoResize(concrete.EntityID);

            if (createCommand.BufferLength > 0)
            {
                Span<ComponentHandle> handles = _createEntityComponents.AsSpan().Slice(createCommand.BufferIndex, createCommand.BufferLength);
                _world.CreateFromHandles(handles);
            }
        }

        while (_deleteEntityBuffer.TryPop(out var item))
        {
            //double check that its alive
            ref var record = ref _world.EntityTable[item.ID];
            if (record.Version == item.Version)
            {
                _world.DeleteEntity(item.ToEntity(_world), ref record);
            }
        }

        while (_removeComponentBuffer.TryPop(out var item))
        {
            var id = item.Entity.ID;
            Entity concrete = item.Entity.ToEntity(_world);
            ref var record = ref _world.EntityTable[id];

            if (record.Version == item.Entity.Version)
            {
                int sparseIndex = item.ComponentID.SparseIndex;
                _world.ComponentRemovedEvent.Invoke(concrete, item.ComponentID);
                GenericEvent? removeEvent = record.HasFlag(EntityFlags.RemoveGenericComp) ?
                    _world.EventLookup.GetValueRefOrNullRef(item.Entity).Remove.GenericEvent :
                    null;

                if (sparseIndex != 0)
                {
                    var set = _world.WorldSparseSetTable[sparseIndex];
                    if (removeEvent is not null)
                        set.InvokeGenericEvent(concrete, removeEvent);
                    set.Remove(id, true);
                }
                else
                {
                    ComponentStorageRecord componentStorage = record.Archetype.GetComponentStorage(item.ComponentID);
                    componentStorage.CallDestroyer(record.Index);
                    if (removeEvent is not null)
                        componentStorage.InvokeGenericActionWith(removeEvent, concrete, record.Index);
                    _world.RemoveArchetypicalComponent(item.Entity.ToEntity(_world), ref record, item.ComponentID);
                }
            }
        }

        while (_addComponentBuffer.TryPop(out var command))
        {
            //TODO: events
            Entity concrete = command.Entity.ToEntity(_world);
            ref var record = ref _world.EntityTable[concrete.EntityID];

            if (record.Version == command.Entity.Version)
            {
                // init x -> generic events -> entity events -> worldevents x
                int sparseIndex = command.ComponentHandle.ComponentID.SparseIndex;
                ComponentStorageRecord? runner = null;
                ComponentSparseSetBase? sparseSet = null;
                int archIndex = 0;

                if (sparseIndex != 0)
                {
                    sparseSet = _world.WorldSparseSetTable[sparseIndex];
                    sparseSet.AddOrSet(concrete.EntityID, command.ComponentHandle);
                    sparseSet.Init(concrete);
                }
                else
                {
                    _world.AddArchetypicalComponent(concrete, ref record, command.ComponentHandle.ComponentID, out var location, out var destination);

                    runner = destination.Components[destination.GetComponentIndex(command.ComponentHandle.ComponentID)];
                    runner.Value.PullComponentFrom(command.ComponentHandle.ParentTable, location.Index, command.ComponentHandle.Index);

                    runner.Value.CallIniter(concrete, location.Index);

                    command.ComponentHandle.Dispose();

                    archIndex = location.Index;
                }

                if (record.HasFlag(EntityFlags.AddComp | EntityFlags.AddGenericComp))
                {
                    ref var events = ref _world.EventLookup.GetValueRefOrNullRef(command.Entity);

                    if(events.Add.GenericEvent is not null)
                    {
                        runner?.InvokeGenericActionWith(events.Add.GenericEvent, concrete, archIndex);
                        sparseSet?.InvokeGenericEvent(concrete, events.Add.GenericEvent);
                    }

                    events.Add.NormalEvent.Invoke(concrete, command.ComponentHandle.ComponentID);
                }

                _world.ComponentAddedEvent.Invoke(concrete, command.ComponentHandle.ComponentID);
            }
        }

        while (_tagEntityBuffer.TryPop(out var command))
        {
            ref var record = ref _world.EntityTable[command.Entity.ID];
            Entity concrete = command.Entity.ToEntity(_world);
            if (record.Version == command.Entity.Version)
            {
                _world.MoveEntityToArchetypeIso(concrete, ref record,
                    Archetype.GetAdjacentArchetypeLookup(_world, ArchetypeEdgeKey.Tag(command.TagID, record.Archetype.ID, ArchetypeEdgeType.AddTag)));

                _world.Tagged.Invoke(concrete, command.TagID);
                if(record.HasFlag(EntityFlags.Detach))
                {
                    _world.EventLookup.GetValueRefOrNullRef(command.Entity).Tag.Invoke(concrete, command.TagID);
                }
            }
        }

        while (_detachTagEntityBuffer.TryPop(out var command))
        {
            ref var record = ref _world.EntityTable[command.Entity.ID];
            Entity concrete = command.Entity.ToEntity(_world);

            if (record.Version == command.Entity.Version)
            {
                _world.MoveEntityToArchetypeIso(command.Entity.ToEntity(_world), ref record,
                    Archetype.GetAdjacentArchetypeLookup(_world, ArchetypeEdgeKey.Tag(command.TagID, record.Archetype.ID, ArchetypeEdgeType.RemoveTag)));

                _world.Detached.Invoke(concrete, command.TagID);
                if (record.HasFlag(EntityFlags.Detach))
                {
                    _world.EventLookup.GetValueRefOrNullRef(command.Entity).Detach.Invoke(concrete, command.TagID);
                }
            }
        }

        _isInactive = true;

        return hasItems;
    }

    private void AssertCreatingEntity()
    {
        if (_lastCreateEntityComponentsBufferIndex < 0)
        {
            Throw();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw() => throw new InvalidOperationException("Use CommandBuffer.Entity() to begin creating an entity!");
    }

    private void SetIsActive()
    {
        _isInactive = false;
    }
}