using Frent.Components;
using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("        ((IComponentRunner<T>)archetype.Components[componentIndicies[Component<T>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = item!;",
    "        ((IComponentRunner<T>)archetype.Components[componentIndicies[Component<T$>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = item!;")]
[Variadic("<T>", "<|T$, |>")]
[Variadic("in T? item = default", "<|T$, |>")]
public partial class World
{
    public Entity Create<T>(in T? item = default)
        where T : IComponent
    {
        Archetype archetype = Archetype<T>.ByWorld[IDAsUInt] ??= Archetype<T>.CreateArchetype(this);
        archetype.CreateEntityLocation(out var eloc);
        byte[] componentIndicies = GlobalWorldTables.ComponentLocationTable[archetype.ArchetypeID];

        //4x deref + cast per component
        ((IComponentRunner<T>)archetype.Components[componentIndicies[Component<T>.ID]]).AsSpan()[eloc.ChunkIndex][eloc.ComponentIndex] = item!;

        return CreateEntityFromLocation(in eloc);
    }
}
