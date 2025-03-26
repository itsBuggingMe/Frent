# Structural Changes
The operations of creating entities, deleting entities, adding components, removing components, tagging entities, detaching tags are known as structual changes.
These operations cannot occur during a world update. A world update begins at the start of any `World.Update` call or system enumeration.
For systems, the world update might end whenever an enumerator is disposed. Multiple systems and `World.Update` calls may be active at the same time, and all of these calls and enumerators must be disposed in order to allow structural changes.

If a structural change method is called during any world update, that change is deferred to the end of the world update. This is different than most other ECS libraries where a command buffer is manually used to defer changes.

When creating entities during a `World.Update` call, deferred entities may or may not be updated on the same method call depending on `Config.UpdateDeferredCreationEntities`. You can tell if an `Entity` refers to a deferred creation entity as it will have the `Disabled` tag and the `DeferredCreate` tags. These tags are removed after the world update and replaced with the proper tags and components.

> [!CAUTION]
> Missing an enumerator dispose can cause the world to be stuck in a state where it never allows structual changes. It is reccomended to stick to the `foreach` syntax and let C# generate the dispose call itself.

```csharp
using World world = new();

// Entity directly created
world.Create<int>(0);

Entity deferred = Entity.Null;
foreach(_ in world.Query<With<int>>()
    .Enumerate<int>())
{
    deferred = world.Create<int>(2);
    //Deferred is a "deferred creation entity"
    //It does not have its components and tags until the end of the world update.

    Debug.Assert(deferred.Tagged<DeferredCreate>());
    Debug.Assert(deferred.Tagged<Disable>());

    //int component is added after enumerator is disposed.
    Debug.Assert(!deferred.Has<int>());
}//enumerator is implicitly disposed after foreach

Debug.Assert(deferred.Get<int>() == 2);
```
