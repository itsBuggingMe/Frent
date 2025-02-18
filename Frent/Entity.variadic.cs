using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frent.Collections;
using Frent.Core;

namespace Frent;


partial struct Entity
{
    public void Add<T>()
    {
        ref EntityLookup thisLookup = ref AssertIsAlive(out World world);
        ref ArchetypeNeighborCache cache = ref NeighborCache<T>.Add.Lookup;
        int index = cache.Traverse(thisLookup.Location.Archetype.ID.RawIndex);

        Archetype destination;

        if(index == 32)
        {
            //TODO: slow path
        }
        else
        {
            destination = new ArchetypeID(cache.Lookup(index)).Archetype(world);
        }
        


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
    }
}
