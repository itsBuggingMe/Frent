# Events

There are a variety of events on `World` and `Entity` you can use.

Events that occur on component add or entity creation are invoked after `IInitable.Init` is invoked.

Events that occur on component remove or entity deletion are invoked after `IDestroyable.Destroy` is invoked.

This ensures that component states viewed through events are valid.

### Generic Events

Generic delegates in C# cannot be unbounded. You cannot have am unbound `entity.OnComponentAdded += (Entity e, T component) => { }` event for example.

We get around this problem with the `IGenericAction<Entity>.Invoke<T>(Entity param, ref T type);` interface.

Frent provides generic events in the form of `Entity.OnComponentRemovedGeneric` and `Entity.OnComponentAddedGeneric`.
