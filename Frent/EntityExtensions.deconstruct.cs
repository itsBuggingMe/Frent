using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// Deconstruction extensions for entities.
/// </summary>
[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>", 8)]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |", 8)]
[Variadic("        comp = GetComp<T>(archetypeTable, comps, eloc);",
    "|        comp$ = GetComp<T$>(archetypeTable, comps, eloc);|", 8)]
public static partial class EntityExtensions
{
    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s).
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have all the components specified.</exception>
    public static void Deconstruct<T>(ref this Entity e, out Ref<T> comp)
    {
        e.AssertIsAlive(out var world, out var eloc);

        Archetype archetype = eloc.Archetype;

        IComponentRunner[] comps = archetype.Components;
        byte[] archetypeTable = archetype.ComponentTagTable;

        comp = GetComp<T>(archetypeTable, comps, eloc);
    }
}

partial class EntityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Ref<TC> GetComp<TC>(byte[] archetypeTable, IComponentRunner[] comps, EntityLocation eloc)
    {
        int compIndex;
        if ((compIndex = archetypeTable[Component<TC>.ID.ID]) >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException<TC>();

        return new Ref<TC>(ref UnsafeExtensions.UnsafeCast<ComponentStorage<TC>>(comps.UnsafeArrayIndex(compIndex))[eloc.Index]);
    }
}
