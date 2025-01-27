using Frent.Core;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("        UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index)).Chunks.UnsafeArrayIndex(eloc.ChunkIndex)[eloc.ComponentIndex] = comp;",
    "|        UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T$>.Index)).Chunks.UnsafeArrayIndex(eloc.ChunkIndex)[eloc.ComponentIndex] = comp$;\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("T comp", "|T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    /// <summary>
    /// Creates an <see cref="Entity"/> with the given component
    /// </summary>
    /// <param name="comp"></param>
    /// <typeparam name="T">The type of the component</typeparam>
    /// <returns>An <see cref="Entity"/> that can be used to acsess the component data</returns>
    public Entity Create<T>(T comp)
    {
        Archetype archetype = Archetype<T>.CreateNewOrGetExistingArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(out var eloc);

        //4x deref per component
        UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(Archetype<T>.OfComponent<T>.Index)).Chunks.UnsafeArrayIndex(eloc.ChunkIndex)[eloc.ComponentIndex] = comp;

        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : new EntityIDOnly(_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = new(eloc, version);
        entity = new Entity(ID, Version, version, id);
        EntityCreated?.Invoke(entity);
        return entity;
    }

    //might remove this due to code size
    /// <summary>
    /// Allocates enough memory for an entity type internally
    /// </summary>
    /// <param name="entityCount">The number of entity slots to allocate for</param>
    /// <typeparam name="T">The sole component type in the entity type to allocate for</typeparam>
    public void EnsureCapacity<T>(int entityCount)
    {
        int id = Archetype<T>.ID.ID;
        EnsureCapacityCore(Archetype<T>.CreateNewOrGetExistingArchetype(this), entityCount);
    }
}