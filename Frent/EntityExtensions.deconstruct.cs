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
[Variadic("        comp = GetComp<T>(archetypeTable, comps, eloc.Index);",
    "|        comp$ = GetComp<T$>(archetypeTable, comps, eloc.Index);|", 8)]
public static partial class EntityExtensions
{
    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s).
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have all the components specified.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct<T>(this Entity e, out Ref<T> comp)
    {
        EntityLocation eloc = e.AssertIsAlive(out _).Location;

        IComponentRunner[] comps = eloc.Archetype.Components;
        byte[] archetypeTable = eloc.Archetype.ComponentTagTable;

        comp = GetComp<T>(archetypeTable, comps, eloc.Index);
    }
}

partial class EntityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Ref<TC> GetComp<TC>(byte[] archetypeTable, IComponentRunner[] comps, int index)
    {
        int compIndex = archetypeTable.UnsafeArrayIndex(Component<TC>.ID.RawIndex) & GlobalWorldTables.IndexBits;
        return new Ref<TC>(ref UnsafeExtensions.UnsafeCast<ComponentStorage<TC>>(comps.UnsafeArrayIndex(compIndex))[index]);
    }
}
