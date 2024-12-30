using Frent.Components;
using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("e<T>", "e<|T$, |>")]
[Variadic("in T? comp = default", "|in T$? comp$ = default, |")]
[Variadic("        ((IComponentRunner<T>)archetype.Components[componentIndicies[Component<T>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;", 
    "|        ((IComponentRunner<T$>)archetype.Components[componentIndicies[Component<T$>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp$!;\n|")]
//it just so happens Archetype and Create both end with "e"
partial class World
{
    public Entity Create<T>(in T? comp = default)
    {
        Archetype archetype = WorldArchetypeTable[Archetype<T>.IDasUInt] ??= Archetype<T>.CreateArchetype(this);
        archetype.CreateEntityLocation(out var eloc);
        byte[] componentIndicies = GlobalWorldTables.ComponentLocationTable[archetype.ArchetypeID];

        //4x deref + cast per component
        ((IComponentRunner<T>)archetype.Components[componentIndicies[Component<T>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = comp!;

        return CreateEntityFromLocation(in eloc);
    }
}
