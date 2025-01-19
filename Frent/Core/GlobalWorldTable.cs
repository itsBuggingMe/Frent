using Frent.Collections;
using System.ComponentModel;

namespace Frent.Core;

internal static class GlobalWorldTables
{
    //we accsess by archetype first because i think we access different comps from the same archetype more
    public static byte[/*archetype id*/][/*component id*/] ComponentTagLocationTable = [];
    internal static int ComponentTagTableBufferSize { get; set; }//reps the length of the second dimension
    internal static Table<World> Worlds = new Table<World>(2);

    internal static readonly object BufferChangeLock = new object();

    internal static void ModifyComponentTagTableIfNeeded(int idValue)
    {
        var table = ComponentTagLocationTable;
        var tableSize = ComponentTagTableBufferSize;
        //when adding a component, we only care about changing the length
        if (tableSize == idValue)
        {
            ComponentTagTableBufferSize = Math.Max(tableSize << 1, 1);
            for (int i = 0; i < table.Length; i++)
            {
                ref var componentsForArchetype = ref table[i];
                Array.Resize(ref componentsForArchetype, ComponentTagTableBufferSize);
                componentsForArchetype.AsSpan(tableSize).Fill(Tag.DefaultNoTag);
            }
        }
    }

    public static int ComponentIndex(ArchetypeID archetype, ComponentID component) => ComponentTagLocationTable.UnsafeArrayIndex(archetype.ID).UnsafeArrayIndex(component.ID) & Tag.IndexBits;
    public static int ComponentIndex(uint archetype, ComponentID component) => ComponentTagLocationTable.UnsafeArrayIndex(archetype).UnsafeArrayIndex(component.ID) & Tag.IndexBits;
    public static bool HasTag(ArchetypeID archetype, TagID tag) => (ComponentTagLocationTable.UnsafeArrayIndex(archetype.ID).UnsafeArrayIndex(tag.ID) & Tag.HasTagMask) != 0;
}