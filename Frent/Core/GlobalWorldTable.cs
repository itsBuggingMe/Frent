using Frent.Collections;

namespace Frent.Core;

internal static class GlobalWorldTables
{
    //we accsess by archetype first because i think we access different comps from the same archetype more
    public static byte[/*archetype id*/][/*component id*/] ComponentLocationTable = [];
    public static Table<World> Worlds = new Table<World>(2);
}