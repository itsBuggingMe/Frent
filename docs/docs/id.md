# Identities

Frent uses several ID and handle types internally and in its public API.

### [ComponentID](/api/Frent.Core.ComponentID.html)

`ComponentID` is a lightweight struct that represents a component type. Internally, Frent uses it to look up component metadata. Using these IDs instead of `Type` objects also makes non-generic operations faster.

For example, `Entity.Get(ComponentID)` should be preferred over `Entity.Get(Type)`, especially since the `Type` can be retrieved at any time with `ComponentID.Type`.

You can get a `ComponentID` instance by inspecting an entity's `Entity.ComponentTypes`, `Component<T>.ID` or `Component.GetComponentID(Type)`.

### [TagID](/api/Frent.Core.TagID.html)

`TagID` is the tag equivalent of `ComponentID`. Just like `ComponentID`, `Entity.Tagged(TagID)` should be preferred over `Entity.Tagged(Type)`.

The method of getting a `TagID` is the same, using `Entity.TagTypes`, `Tag<T>.ID`, or `Tag.GetTagID(Type)`.

### [Component Handles](/api/Frent.Core.ComponentHandle.html)

Component handles store structs without boxing. They are not necessarily faster than boxing, but they can reduce GC pressure when calling non-generic APIs such as `Entity.AddFromHandles` or `World.CreateFromHandles`.

You create a component handle with `ComponentHandle.Create<T>(in T)`.

You must dispose of component handles manually. Losing a handle without disposing it leaks its storage.
