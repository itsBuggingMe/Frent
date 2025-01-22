using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Collections;

namespace Frent.Core;

public class CommandBuffer
{
    private static int _activeCommandBuffers;

    internal FastStack<EntityIDOnly> _deleteEntityBuffer = FastStack<EntityIDOnly>.Create(4);
    internal FastStack<AddComponent> _addComponentBuffer = FastStack<AddComponent>.Create(4);
    internal FastStack<DeleteComponent> _removeComponentBuffer = FastStack<DeleteComponent>.Create(4);
    internal FastStack<CreateCommand> _createEntityBuffer = FastStack<CreateCommand>.Create(4);
    internal FastStack<(ComponentID, int index)> _createEntityComponents = FastStack<(ComponentID, int index)>.Create(4);
    internal World _world;
    //-1 indicates normal state
    internal int _lastCreateEntityComponentsBufferIndex = -1;
    internal bool _isInactive;

    public bool HasBufferItems => !_isInactive;

    public CommandBuffer(World world)
    {
        _world = world;
    }

    public void DeleteEntity(Entity entity)
    {
        SetIsActive();
        _deleteEntityBuffer.Push(entity.EntityIDOnly);
    }
    
    public void RemoveComponent(ComponentID component, Entity entity)
    {
        SetIsActive();
        _removeComponentBuffer.Push(new DeleteComponent(entity.EntityIDOnly, component));
    }

    public void RemoveComponent<T>(Entity entity) => RemoveComponent(Component<T>.ID, entity);

    public void RemoveComponent(Type type, Entity entity) => RemoveComponent(Component.GetComponentID(type), entity);

    public void AddComponent<T>(Entity entity, T component)
    {
        SetIsActive();
        Component<T>.TrimmableStack.PushStronglyTyped(in component, out int index);
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, Component<T>.ID, index));
    }

    public void AddComponent(Entity entity, ComponentID componentID, object component)
    {
        SetIsActive();
        int index = Component.ComponentTable[componentID.ID].Stack.Push(component);
        _addComponentBuffer.Push(new AddComponent(entity.EntityIDOnly, componentID, index));
    }

    public void AddComponent(Entity entity, Type componentType, object component) => AddComponent(entity, Component.GetComponentID(componentType), component);
    public void AddComponent(Entity entity, object component) => AddComponent(entity, component.GetType(), component);

    public CommandBuffer Entity()
    {
        if(_lastCreateEntityComponentsBufferIndex >= 0)
        {
            throw new InvalidOperationException("An entity is currently being created! Use 'End' to finish an entity creation!");
        }
        _lastCreateEntityComponentsBufferIndex = _createEntityComponents.Count;
        return this;
    }

    public CommandBuffer With<T>(T component)
    {
        Component<T>.TrimmableStack.PushStronglyTyped(component, out int index);
        _createEntityComponents.Push((Component<T>.ID, index));
        return this;
    }

    public void End()
    {
        //CreateCommand points to a segment of the _createEntityComponents stack
        _createEntityBuffer.Push(new CreateCommand(
            _lastCreateEntityComponentsBufferIndex, 
            _createEntityBuffer.Count - _lastCreateEntityComponentsBufferIndex));
        _lastCreateEntityComponentsBufferIndex = -1;
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