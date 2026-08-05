# Events

There are a variety of events on `World` and `Entity` you can use.

Events for component addition and entity creation are invoked after `IInitable.Init`.

Events for component removal and entity deletion are invoked before `IDestroyable.Destroy`. This allows an event handler to inspect the component while it is still valid.

This ensures that component states viewed through events are valid.

### Generic Events

Generic delegates in C# cannot be unbounded. You cannot have am unbound `entity.OnComponentAdded += (Entity e, T component) => { }` event for example.

Frent handles this with the `IGenericAction<Entity>.Invoke<T>(Entity param, ref T type)` interface.

Frent provides generic events in the form of `Entity.OnComponentRemovedGeneric` and `Entity.OnComponentAddedGeneric`.
