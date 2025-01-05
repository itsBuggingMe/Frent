using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("        ((IComponentRunner<T>)archetype.Components[Archetype<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;",
    "|        ((IComponentRunner<T$>)archetype.Components[Archetype<T>.OfComponent<T$>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp$!;\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("in T comp", "|in T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    public Entity Create<T>(in T comp)
    {
        Archetype archetype = Archetype<T>.GetExistingOrCreateNewArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(out var eloc);

        //4x deref + cast per component
        ((IComponentRunner<T>)archetype.Components[Archetype<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;

        return entity = CreateEntityFromLocation(in eloc);
    }

    //might remove this due to code size
    public void EnsureCapacity<T>(int entityCount)
    {
        EnsureCapacityCore(WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.GetExistingOrCreateNewArchetype(this), entityCount);
    }
}