using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frent.Collections;
using Frent.Core;
using Frent.Updating;

namespace Frent;


partial struct Entity
{
    //traversing archetype graph strategy:
    //1. hit small & fast static per type cache - 1 branch
    //2. hit per world cache - hardware accelerated
    //3. dictionary lookup
    //4. create new archetype
    public void Add<T>()
    {
        ref EntityLookup thisLookup = ref AssertIsAlive(out World world);
        ref ArchetypeNeighborCache cache = ref NeighborCache<T>.Add.Lookup;
        int index = cache.Traverse(thisLookup.Location.Archetype.ID.RawIndex);

        Archetype from = thisLookup.Location.Archetype;
        Archetype destination;

        if(index == 32)
        {
            //TODO: slow path
            destination = null!;
        }
        else
        {
            destination = world.WorldArchetypeTable.UnsafeArrayIndex(cache.Lookup(index));
        }
        
        //ReadOnlySpan<IComponentRunner> runners = world.MoveEntityToArchetypeAdd(from, destination, out);

        throw new NotImplementedException();
    }

    //public void Remove<T>()
    //{
    //
    //}

    private static class NeighborCache<T>
    {
        //separate into individual classes to avoid creating uneccecary static classes.
        internal static class Add
        {
            internal static ArchetypeNeighborCache Lookup;
        }

        internal static class Remove
        {
            internal static ArchetypeNeighborCache Lookup;
        }

        internal static class Tag
        {
            internal static ArchetypeNeighborCache Lookup;
        }

        internal static class Detach
        {
            internal static ArchetypeNeighborCache Lookup;
        }

        public static Archetype TraverseThroughCacheOrAdd(Entity entity, ref ArchetypeNeighborCache cache, Archetype.ArchetypeStructualAction action)
        {
            ref EntityLookup thisLookup = ref entity.AssertIsAlive(out World world);
            int index = cache.Traverse(thisLookup.Location.Archetype.ID.RawIndex);

            Archetype destination;

            if (index == 32)
            {
                
            }
            else
            {
                destination = world.WorldArchetypeTable.UnsafeArrayIndex(cache.Lookup(index));
            }
        }
    }
}
