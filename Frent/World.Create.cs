using Frent.Core;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("        UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index] = comp;",
    "|        UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index))[eloc.Index] = comp$;\n|")]
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
    public Entity Create<T>(T comp)
    {
        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(EntityFlags.None, out var eloc);

        //1x deref per component
        UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index))[eloc.Index] = comp;

        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        var (id, version) = entity = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, 0);
        EntityTable[id] = new(eloc, version);
        Entity concreteEntity = new Entity(ID, version, id);
        EntityCreatedEvent.Invoke(concreteEntity);
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