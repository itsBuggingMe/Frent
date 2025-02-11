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
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }

    public ref T GetSafeAnd<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);

        int num = MemoryHelpers.BoolToByte(location.Version == entity.EntityVersion & PackedIDVersion == entity.PackedWorldInfo);

        Archetype archetype = location.Location.Archetype;
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex * num));
        return ref storage[location.Location.Index];
    }

    public ref T GetSafeOr<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);
        if(location.Version != entity.EntityVersion | PackedIDVersion != entity.PackedWorldInfo)
            FrentExceptions.Throw_InvalidOperationException("entity dead");

        Archetype archetype = location.Location.Archetype;
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }

    public ref T GetSafeIf<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);
        if(location.Version != entity.EntityVersion)
            FrentExceptions.Throw_InvalidOperationException("entity dead");
        if(PackedIDVersion != entity.PackedWorldInfo)
            FrentExceptions.Throw_InvalidOperationException("world dead");

        Archetype archetype = location.Location.Archetype;
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }

    public ref T GetSafeIfRem<T>(Entity entity)
    {
        //world valid x, entity valid x, has component

        EntityLookup location = EntityTable.UnsafeIndexNoResize(entity.EntityID);
        if(PackedIDVersion != entity.PackedWorldInfo)
            FrentExceptions.Throw_InvalidOperationException("world dead");

        Archetype archetype = location.Location.Archetype;
        
        int compIndex = archetype.ComponentTagTable.UnsafeArrayIndex(Component<T>.ID.ID) & GlobalWorldTables.IndexBits;

        //Components[0] null; trap
        ComponentStorage<T> storage = UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(archetype.Components.UnsafeArrayIndex(compIndex));
        return ref storage[location.Location.Index];
    }
}