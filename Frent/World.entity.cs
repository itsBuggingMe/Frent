using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating.Runners;

namespace Frent;

partial class World
{
    public ref T GetUnsafe<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);

        //world + entity valid hardware trap
        Archetype archetype = location.Location.Archetype;
        
        int compIndex = archetype.GetComponentIndex<T>();

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }
}