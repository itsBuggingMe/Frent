# Structural Changes
The operations of deleting entities, adding components, removing components, tagging entities, detaching tags are known as structural changes.
These operations cannot occur during a world update. A world update begins at the start of any `World.Update` call or system enumeration.
For systems, the world update might end whenever an enumerator is disposed. Multiple systems and `World.Update` calls may be active at the same time, and all of these calls and enumerators must be disposed in order to allow structural changes.

If a structural change method is called during any world update, that change is deferred to the end of the world update. This is different than most other ECS libraries where a command buffer is manually used to defer changes.

When creating entities during a `World.Update` call, these entities may or may not be updated on the same method call depending on `Config.UpdateDeferredCreationEntities`.

> [!CAUTION]
> Missing an enumerator dispose can cause the world to be stuck in a state where it never allows structural changes. It is reccomended to stick to the `foreach` syntax and let C# generate the dispose call itself.

```csharp
using World world = new();

// Create an entity so the system runs at least once.
Entity entity = world.Create<int>(0);

Entity deferred = Entity.Null;
foreach (var _ in world.Query<With<int>>()
    .Enumerate<int>())
{
    // Frent allows you to directly create entities during updates and systems
    deferred = world.Create<int>(2);
    int comp = deferred.Get<int>(); // 2

    // Other changes, like delete, are deferred.
    deferred.Delete();
    _ = deferred.IsAlive; // true
}// enumerator is implicitly disposed after foreach; changes are applied

_ = deferred.IsAlive // false
```