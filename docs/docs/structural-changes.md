# Structural Changes

Some operations are structural changes, which means that the internal data structures may be altered. To ensure safety, there is different behavior if a `Update` call or system is currently active.

| Action           | Behavior        |
|------------------|-----------------|
| Create Entity    | Fully Supported |
| Delete Entity    | Auto Deferred*  |
| Add Component    | Auto Deferred   |
| Remove Component | Auto Deferred   |
| Tag Tag          | Auto Deferred   |
| Detach Tag       | Auto Deferred   |

\* May have support in the future. 

Creating entities is fully supported during systems and updates. When creating entities during a `World.Update`, you can require the components on these entities to be updated by setting `Config.UpdateDeferredCreationEntities` to true and passing a config into the world.

In addition, since structural changes may change internal structures, all `ref`s and `Ref<T>`s should be treated as outdated after making a call that causes a structural change.

Structural changes that are auto deferred are saved and only applied once all systems and `Update` methods finish.

> [!CAUTION]
> Missing an enumerator dispose when enumerating a query can cause the world to be stuck in a state where it never applies structural changes. It is reccomended to stick to the `foreach` syntax and let C# generate the dispose call itself.