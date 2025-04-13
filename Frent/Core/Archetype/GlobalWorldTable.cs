using Frent.Collections;
using System.Security.AccessControl;

namespace Frent.Core.Structures;

internal static class GlobalWorldTables
{
    //we accsess by archetype first because i think we access different comps from the same archetype more
    public static byte[/*archetype id*/][/*component id*/] ComponentTagLocationTable = [];
    internal static int ComponentTagTableBufferSize { get; set; }//reps the length of the second dimension
    internal static Table<World> Worlds = new Table<World>(2);

    internal static readonly object BufferChangeLock = new object();

    //each byte contains the data as follows:
    // 1 bit Tag exists -> Lookup by tag ID
    // 3 bits - nothing
    // 4 bits - index of component (1111) -> Lookup by component ID

    public const byte HasTagMask = 0b_1000_0000;
    public const byte DefaultNoTag = 0b_0000_0000;
    public const byte IndexBits = 0b_0111_1111;
    public const int Mod16Mask = 0xF;

    internal static void GrowComponentTagTableIfNeeded(int idValue)
    {
        var table = ComponentTagLocationTable;
        var tableSize = ComponentTagTableBufferSize;
        var worlds = Worlds.AsSpan();

        //when adding a component, we only care about changing the length
        if (tableSize == idValue)
        {
            ComponentTagTableBufferSize = Math.Max(tableSize << 1, 1);
            for (int i = 0; i < table.Length; i++)
            {
                ref var componentsForArchetype = ref table[i];
                Array.Resize(ref componentsForArchetype, ComponentTagTableBufferSize);

                //componentsForArchetype.AsSpan(tableSize).Fill(DefaultNoTag);

                //update world archetypes
                foreach (var world in worlds)
                {
                    if (world is not null)
                    {
                        ref var tableItem = ref world.WorldArchetypeTable[i];
                        if(tableItem.Archetype is not null)
                        {
                            tableItem.Archetype.ComponentTagTable = componentsForArchetype;
                            tableItem.DeferredCreationArchetype.ComponentTagTable = componentsForArchetype;
                        }
                    }
                }
            }
        }
    }

    public static int ComponentIndex(ArchetypeID archetype, ComponentID component) => ComponentTagLocationTable.UnsafeArrayIndex(archetype.RawIndex).UnsafeArrayIndex(component.RawIndex) & IndexBits;
    public static bool HasTag(ArchetypeID archetype, TagID tag) => (ComponentTagLocationTable.UnsafeArrayIndex(archetype.RawIndex).UnsafeArrayIndex(tag.RawValue) & HasTagMask) != 0;
}