using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Collections;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating.Runners;

namespace Frent.Core;

/// <summary>
/// Stores a set of structual changes that can be applied to a <see cref="World"/>
/// </summary>
public class CommandBuffer
{
    private static int _activeCommandBuffers;

    internal static Action? ClearTempComponentStorage;

    internal FastStack<EntityIDOnly> _deleteEntityBuffer = FastStack<EntityIDOnly>.Create(4);
    internal FastStack<AddComponent> _addComponentBuffer = FastStack<AddComponent>.Create(4);
    internal FastStack<DeleteComponent> _removeComponentBuffer = FastStack<DeleteComponent>.Create(4);
    internal FastStack<CreateCommand> _createEntityBuffer = FastStack<CreateCommand>.Create(4);
    internal FastStack<(ComponentID, int index)> _createEntityComponents = FastStack<(ComponentID, int index)>.Create(4);
    internal World _world;
    //-1 indicates normal state
    internal int _lastCreateEntityComponentsBufferIndex = -1;
    internal bool _isInactive;

    /// <summary>
    /// Whether or not the buffer currently has items to be played back
    /// </summary>
    public bool HasBufferItems => !_isInactive;

    /// <summary>
    /// Creates a command buffer, which stores changes to a world without directly applying them
    /// </summary>
    /// <param name="world">The world to apply things too</param>
    public CommandBuffer(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Deletes a component from when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity that will be deleted on playback</param>
    public void DeleteEntity(Entity entity)
    {
        SetIsActive();
        _deleteEntityBuffer.Push(entity.EntityIDOnly);
    }
    
    /// <summary>
    /// Removes a component from when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity to remove a component from</param>
    /// <param name="component">The component to remove</param>
    public void RemoveComponent(Entity entity, ComponentID component)
    {
        SetIsActive();
        _removeComponentBuffer.Push(new DeleteComponent(entity.EntityIDOnly, component));
    }

    /// <summary>
    /// Removes a component from when <see cref="PlayBack"/> is called
    /// </summary>
    /// <typeparam name="T">The component type to remove</typeparam>
    /// <param name="entity">The entity to remove a component from</param>
    public void RemoveComponent<T>(Entity entity) => RemoveComponent(entity, Component<T>.ID);

    /// <summary>
    /// Removes a component from when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity to remove a component from</param>
    /// <param name="type">The type of component to remove</param>
    public void RemoveComponent(Entity entity, Type type) => RemoveComponent(entity, Component.GetComponentID(type));

    /// <summary>
    /// Adds a component to an entity when <see cref="PlayBack"/> is called
    /// </summary>
    /// <typeparam name="T">The component type to add</typeparam>
    /// <param name="entity">The entity to add to</param>
    /// <param name="component">The component to add</param>
    public void AddComponent<T>(Entity entity, T component)
    {
        SetIsActive();
        Component<T>.TrimmableStack.PushStronglyTyped(in component, out int index);
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, Component<T>.ID, index));
    }

    /// <summary>
    /// Adds a component to an entity when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity to add to</param>
    /// <param name="component">The component to add</param>
    /// <param name="componentID">The ID of the component type to add as</param>
    /// <remarks><paramref name="component"/> must be assignable to <see cref="ComponentID.Type"/></remarks>
    public void AddComponent(Entity entity, ComponentID componentID, object component)
    {
        SetIsActive();
        int index = Component.ComponentTable[componentID.ID].Stack.Push(component);
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, componentID, index));
    }

    /// <summary>
    /// Adds a component to an entity when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity to add to</param>
    /// <param name="component">The component to add</param>
    /// <param name="componentType">The type to add the component as</param>
    /// <remarks><paramref name="component"/> must be assignable to <paramref name="Type"/></remarks>
    public void AddComponent(Entity entity, Type componentType, object component) => AddComponent(entity, Component.GetComponentID(componentType), component);
    
    /// <summary>
    /// Adds a component to an entity when <see cref="PlayBack"/> is called
    /// </summary>
    /// <param name="entity">The entity to add to</param>
    /// <param name="component">The component to add</param>
    public void AddComponent(Entity entity, object component) => AddComponent(entity, component.GetType(), component);

    #region Create
    public CommandBuffer Entity()
    {
        SetIsActive();
        if(_lastCreateEntityComponentsBufferIndex >= 0)
        {
            throw new InvalidOperationException("An entity is currently being created! Use 'End' to finish an entity creation!");
        }
        _lastCreateEntityComponentsBufferIndex = _createEntityComponents.Count;
        return this;
    }

    public CommandBuffer With<T>(T component)
    {
        AssertCreatingEntity();
        Component<T>.TrimmableStack.PushStronglyTyped(component, out int index);
        _createEntityComponents.Push((Component<T>.ID, index));
        return this;
    }

    public CommandBuffer WithBoxed(ComponentID componentID, object component)
    {
        AssertCreatingEntity();
        //we don't check IsAssignableTo - reason is perf - InvalidCastException anyways
        int index = Component.ComponentTable[componentID.ID].Stack.Push(componentID);
        _createEntityComponents.Push((componentID, index));
        return this;
    }

    public CommandBuffer WithBoxed(object component) => WithBoxed(component.GetType(), component);

    public CommandBuffer WithBoxed(Type type, object component) => WithBoxed(Component.GetComponentID(type), component);

    public void End()
    {
        //CreateCommand points to a segment of the _createEntityComponents stack
        _createEntityBuffer.Push(new CreateCommand(
            _lastCreateEntityComponentsBufferIndex, 
            _createEntityBuffer.Count - _lastCreateEntityComponentsBufferIndex));
        _lastCreateEntityComponentsBufferIndex = -1;
    }
    #endregion

    /// <summary>
    /// Removes all commands without playing them back
    /// </summary>
    public void Clear()
    {
        _isInactive = false;
        _createEntityBuffer.Clear();
        _removeComponentBuffer.Clear();
        _deleteEntityBuffer.Clear();

        if (Interlocked.Decrement(ref _activeCommandBuffers) == 0)
        {
            ClearTempComponentStorage?.Invoke();
        }
    }

    /// <summary>
    /// Plays all the queued commands, applying them to a world
    /// </summary>
    /// <returns><see langword="true"/> when at least one change was made; <see langword="false"/> when this command buffer is empty and not active.</returns>
    public bool PlayBack()
    {
        bool flag = false;

        while (_removeComponentBuffer.TryPop(out var item))
        {
            flag = true;
            var id = (uint)item.Entity.ID;
            var record = _world.EntityTable[id];
            if (record.Version == item.Entity.Version)
            {
                _world.RemoveComponent(item.Entity.ToEntity(_world), record.Location, item.ComponentID);
            }
        }

        while (_addComponentBuffer.TryPop(out var command))
        {
            flag = true;
            var id = (uint)command.Entity.ID;
            var record = _world.EntityTable[id];
            if (record.Version == command.Entity.Version)
            {
                Entity concrete = command.Entity.ToEntity(_world);
                var archetype = _world.AddComponent(concrete, record.Location, command.ComponentID,
                    out var runner,
                    out var location);
                runner.PullComponentFrom(Component.ComponentTable[command.ComponentID.ID].Stack, location, command.Index);
                OnComponentAdded.TryInvokeAction(archetype, runner, concrete, command.ComponentID, location.ChunkIndex, location.ComponentIndex);
            }
        }

        while (_deleteEntityBuffer.TryPop(out var item))
        {
            flag = true;
            //double check that its alive
            var record = _world.EntityTable[(uint)item.ID];
            if (record.Version == item.Version)
            {
                _world.DeleteEntity(item.ToEntity(_world), record.Location);
            }
        }

        while (_createEntityBuffer.TryPop(out CreateCommand createCommand))
        {
            throw new NotImplementedException();
        }

        if (Interlocked.Decrement(ref _activeCommandBuffers) == 0)
        {
            ClearTempComponentStorage?.Invoke();
        }

        _isInactive = false;

        return flag;
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
        if(_isInactive)
        {
            _isInactive = false;
            Interlocked.Increment(ref _activeCommandBuffers);
        }
    }
}