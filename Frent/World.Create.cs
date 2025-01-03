using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;

namespace Frent;

[Variadic("        ((IComponentRunner<T>)archetype.Components[ArchetypeConstantCache<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;",
    "|        ((IComponentRunner<T$>)archetype.Components[ArchetypeConstantCache<T>.OfComponent<T$>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp$!;\n|")]
[Variadic("e<T>", "e<|T$, |>")]
[Variadic("y<T>", "y<|T$, |>")]
[Variadic("in T comp", "|in T$ comp$, |")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    public Entity Create<T>(in T comp)
    {
        Archetype archetype = WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.CreateArchetype(this);
        ref var entity = ref archetype.CreateEntityLocation(out var eloc);

        //4x deref + cast per component
        ((IComponentRunner<T>)archetype.Components[ArchetypeConstantCache<T>.OfComponent<T>.Index]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;

        return entity = CreateEntityFromLocation(in eloc);
    }

    public void EnsureCapacity<T>(int entityCount)
    {
        Archetype archetype = WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.CreateArchetype(this);
        archetype.EnsureCapacity(entityCount);
    }

    //refactor this into archetype
    private class ArchetypeConstantCache<T>
    {
        internal class OfComponent<C>
        {
            public static readonly int Index = GlobalWorldTables.ComponentLocationTable[Archetype<T>.ID][Component<C>.ID];
        }
    }
}
