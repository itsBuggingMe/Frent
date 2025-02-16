using Frent.Components;
using Frent.Core;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index]; ref1 = comp;",
    "|        ref T$ ref$ = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index))[eloc.Index]; ref$ = comp$;\n|")]
[Variadic("        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);",
    "|        Component<T$>.Initer?.Invoke(concreteEntity, ref ref$);\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("T comp", "|T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    /// <summary>
    /// Creates an <see cref="Entity"/> with the given component(s)
    /// </summary>
    /// <returns>An <see cref="Entity"/> that can be used to acsess the component data</returns>
    [SkipLocalsInit]
    public Entity Create<T>(T comp)
    {
        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(EntityFlags.None, out var eloc);
        var components = archetype.Components;

        //1x lookup per component
        ref T ref1 = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index]; ref1 = comp;

        var (id, version) = entity = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, 0);
        EntityTable[id] = new(eloc, version);
        Entity concreteEntity = new Entity(ID, version, id);
        EntityCreatedEvent.Invoke(concreteEntity);

        Component<T>.Initer?.Invoke(concreteEntity, ref ref1);

        return concreteEntity;
    }

    //might remove this due to code size
    /// <summary>
    /// Allocates enough memory for an entity type internally
    /// </summary>
    /// <param name="entityCount">The number of entity slots to allocate for</param>
    public void EnsureCapacity<T>(int entityCount)
    {
        int id = Archetype<T>.ID.ID;
        EnsureCapacityCore(Archetype<T>.CreateNewOrGetExistingArchetype(this), entityCount);
    }
}