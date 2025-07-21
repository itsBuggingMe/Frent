# Identities

Frent uses many ID/Handle types and values internally and on the public api.

### [ComponentID](/api/Frent.Core.ComponentID.html)

The `ComponentID` is a lightweight struct that represents a component type. Internally, it's an ID used to lookup various metadata about components, and it is useful to hold these instead of `Type` objects when using non-generic methods for faster operations.

For example, `Entity.Get(ComponentID)` should be preferred over `Entity.Get(Type)`, especially since the `Type` can be retrieved at any time with `ComponentID.Type`.

You can get a `ComponentID` instance by inspecting an entity's `Entity.ComponentTypes`, `Component<T>.ID` or `Component.GetComponentID(Type)`.

### [TagID](/api/Frent.Core.TagID.html)

`TagID` is the tag equivalent to the `ComponentID`. Just like `ComponentID`, `Entity.Tagged(TagId)` should be preferred over `Entity.Tagged(Type)`.

The method of getting a `TagID` is the same, using `Entity.TagTypes`, `Tag<T>.ID`, or `Tag.GetTagID(Type)`.

### [Component Handles](/api/Frent.Core.ComponentHandle.html)

Component Handles are a way to store structs without boxing. Note that it is not necessarily faster, as allocating a box is very fast. Rather, component handles are reccomended as a way to reduce GC pressure when calling non-generic apis like `Entity.AddFromHandles` or `World.CreateFromHandles`.

You create a component handle with `ComponentHandle.Create<T>(in T)`.

You must dispose of component handles manually. Creating and losing a component handle without disposing will result in memory leaks.