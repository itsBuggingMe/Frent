using Frent.Collections;
using Frent.Core;
using Frent.Updating;
using Frent.Variadic.Generator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

/// <summary>
/// Deconstruction extensions for entities.
/// </summary>
[Variadic("Deconstruct<T>", "Deconstruct<|T$, |>", 8)]
[Variadic("out Ref<T> comp", "|out Ref<T$> comp$, |")]
[Variadic("        comp = Component<T>.IsSparseComponent ? MemoryHelpers.GetSparseSet<T>(ref first).GetUnsafe(e.EntityID) : GetComp<T>(archetypeTable, comps, eloc.Index);",
    "|        comp$ = Component<T$>.IsSparseComponent ? MemoryHelpers.GetSparseSet<T$>(ref first).GetUnsafe(e.EntityID) : GetComp<T$>(archetypeTable, comps, eloc.Index);\n|")]
[Variadic("if (Component<T>.IsSparseComponent)", "if (|Component<T$>.IsSparseComponent || |false)")]
[Variadic("<T>", "<|T$, |>")]
public static partial class EntityExtensions
{
    /// <summary>
    /// Deconstructs the constituent components of an entity as reference(s).
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not alive.</exception>
    /// <exception cref="ComponentNotFoundException">The entity does not have all the components specified.</exception>
    /// <variadic />
    public static void Deconstruct<T>(this Entity e, out Ref<T> comp)
    {
        EntityLocation eloc = e.AssertIsAlive(out World w);

        ComponentStorageRecord[] comps = eloc.Archetype.Components;
        byte[] archetypeTable = eloc.Archetype.ComponentTagTable;

        ref ComponentSparseSetBase first = ref Unsafe.NullRef<ComponentSparseSetBase>();
        if (Component<T>.IsSparseComponent)
        {
            first = ref MemoryMarshal.GetArrayDataReference(w.WorldSparseSetTable);
            Bitset.AssertHasSparseComponents(ref eloc.GetBitset(), ref Unsafe.AsRef(in BitsetHelper<T>.BitsetOf));
        }

        comp = Component<T>.IsSparseComponent ? MemoryHelpers.GetSparseSet<T>(ref first).GetUnsafe(e.EntityID) : GetComp<T>(archetypeTable, comps, eloc.Index);
    }
}

partial class EntityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Ref<TC> GetComp<TC>(byte[] archetypeTable, ComponentStorageRecord[] comps, int index)
    {
        int compIndex = archetypeTable.UnsafeArrayIndex(Component<TC>.ID.RawIndex) & GlobalWorldTables.IndexBits;
        return new Ref<TC>(UnsafeExtensions.UnsafeCast<TC[]>(comps.UnsafeArrayIndex(compIndex).Buffer), index);
    }
}
