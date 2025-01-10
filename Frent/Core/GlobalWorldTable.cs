using Frent.Collections;

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

    public static int ComponentIndex(ArchetypeID archetype, ComponentID component) => ComponentTagLocationTable[archetype.ID][component.ID] & Tag.IndexBits;
    public static int ComponentIndex(uint archetype, ComponentID component) => ComponentTagLocationTable[archetype][component.ID] & Tag.IndexBits;
    public static bool HasTag(ushort archetype, TagID tag) => (ComponentTagLocationTable[archetype][tag.ID] & Tag.HasTagMask) != 0;
}