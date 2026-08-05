# Structural Changes

Some operations are structural changes, which means that Frent's internal data structures may be altered. To ensure safety, these operations behave differently while a `World.Update` call or system is active.

| Action           | Behavior        |
|------------------|-----------------|
| Create Entity    | Fully Supported |
| Delete Entity    | Auto Deferred*  |
| Add Component    | Auto Deferred   |
| Remove Component | Auto Deferred   |
| Tag Tag          | Auto Deferred   |
| Detach Tag       | Auto Deferred   |
| Link Entities    | Auto Deferred   |
| Unlink Entities  | Auto Deferred   |

Creating entities is fully supported during systems and updates. To update entities during the same `World.Update` call in which they are created, set `World.UpdateDeferredCreationEntities` to `true` or pass `updateDeferredCreationEntities: true` to the `World` constructor.

Because structural changes may reorganize internal storage, treat every `ref T` and `Ref<T>` from that world as outdated after making a structural change. Retrieve the component again before using it.

Structural changes that are auto deferred are saved and only applied once all systems and `Update` methods finish.

> [!CAUTION]
> Missing an enumerator dispose when enumerating a query can cause the world to be stuck in a state where it never applies structural changes. It is reccomended to stick to the `foreach` syntax and let C# generate the dispose call itself.