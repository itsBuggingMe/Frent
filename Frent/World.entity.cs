using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating.Runners;

namespace Frent;

partial class World
{
    public ref T Get<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);
        
        int worldValid = MemoryHelpers.BoolToByte(entity.PackedWorldInfo == PackedIDVersion) 
            * MemoryHelpers.BoolToByte(entity.EntityVersion == location.Version);

        //world + entity valid hardware trap
        Archetype archetype = WorldArchetypeTable.UnsafeArrayIndex(location.Location.ArchetypeID.ID * worldValid);
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }
}
