using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ((IComponentRunner<T>)archetype.Components[Archetype<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;",
    "|        ((IComponentRunner<T$>)archetype.Components[Archetype<T>.OfComponent<T$>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp$!;\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("in T comp", "|in T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T>(in T comp)
    {
        Archetype archetype = WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.CreateNewArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(out var eloc);

        //4x deref + cast per component
        ((IComponentRunner<T>)archetype.Components[Archetype<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;

        //manually inlined from World.CreateEntityFromLocation
        //The jit likes to inline the outer create function and not inline
        //the inner functions - benchmarked to improve perf by 10-20%
        //also i have no clue why, but according to my benchmarks, not putting any method
        //impl attribute on this method is 2x as slow as putting either AggInlining, or NoInlining

        var (id, version) = _recycledEntityIds.TryPop(out var v) ? v : (_nextEntityID++, (ushort)0);
        EntityTable[(uint)id] = (eloc, version);
        return entity = new Entity(ID, Version, version, id);
    }

    //might remove this due to code size
    public void EnsureCapacity<T>(int entityCount)
    {
        EnsureCapacityCore(WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.CreateNewArchetype(this), entityCount);
    }
}