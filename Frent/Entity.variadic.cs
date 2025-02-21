using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;

namespace Frent;


partial struct Entity
{
    //traversing archetype graph strategy:
    //1. hit small & fast static per type cache - 1 branch
    //2. dictionary lookup
    //3. create new archetype
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddNew<T>(in T c1)
    {
        ref EntityLookup thisLookup = ref AssertIsAlive(out World world);

        Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
            world, 
            ref NeighborCache<T>.Add.Lookup, 
            ref thisLookup, 
            true);

        Span<IComponentRunner> buff = [null!];
        world.MoveEntityToArchetypeAdd(buff, this, ref thisLookup, out EntityLocation nextLocation, to);

        ((ComponentStorage<T>)buff[0])[nextLocation.Index] = c1;

        EntityFlags flags = thisLookup.Location.Flags | world.WorldEventFlags;
        if(EntityLocation.HasEventFlag(flags, EntityFlags.AddComp | EntityFlags.WorldAddComp))
        {
            world.ComponentAddedEvent.Invoke(this, Component<T>.ID);
            if(EntityLocation.HasEventFlag(flags, EntityFlags.AddComp))
            {
                ref EventRecord eventRecord = ref CollectionsMarshal.GetValueRefOrNullRef(world.EventLookup, EntityIDOnly);

                eventRecord.Add.NormalEvent.Invoke(this, Component<T>.ID);
                buff[0].InvokeGenericActionWith(eventRecord.Add.GenericEvent, this, nextLocation.Index);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveNew<T>()
    {
        ref EntityLookup thisLookup = ref AssertIsAlive(out World world);

        Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Remove.Lookup,
            ref thisLookup,
            false);

        Span<IComponentRunner> runners = [null!];
        world.MoveEntityToArchetypeRemove(runners, this, ref thisLookup, out var nextLocation, to);
    }

    private struct NeighborCache<T> : IArchetypeGraphEdge
    {
        public void ModifyTags(ref ImmutableArray<TagID> tags, bool add)
        {
            if(add)
            {
                tags = MemoryHelpers.Concat(tags, Core.Tag<T>.ID);
            }
            else
            {
                tags = MemoryHelpers.Remove(tags, Core.Tag<T>.ID);
            }
        }

        public void ModifyComponents(ref ImmutableArray<ComponentID> components, bool add)
        {
            if (add)
            {
                components = MemoryHelpers.Concat(components, Component<T>.ID);
            }
            else
            {
                components = MemoryHelpers.Remove(components, Component<T>.ID);
            }
        }

        public int TypeCount => 1;

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

partial struct Entity
{
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Archetype TraverseThroughCacheOrCreate<T, TEdge>(
        World world,
        ref ArchetypeNeighborCache cache,
        ref EntityLookup currentLookup,
        bool add)
            where T : ITypeID
            where TEdge : struct, IArchetypeGraphEdge
    {
        ArchetypeID archetypeFromID = currentLookup.Location.ArchetypeID;
        int index = cache.Traverse(archetypeFromID.RawIndex);

        Archetype destination;

        if (index == 32)
        {
            destination = NotInCache(world, ref cache, archetypeFromID, add);
        }
        else
        {
            destination = world.WorldArchetypeTable.UnsafeArrayIndex(cache.Lookup(index));
        }

        static Archetype NotInCache(World world, ref ArchetypeNeighborCache cache, ArchetypeID archetypeFromID, bool add)
        {
            ImmutableArray<ComponentID> componentIDs = archetypeFromID.Types;
            ImmutableArray<TagID> tagIDs = archetypeFromID.Tags;

            if (typeof(T) == typeof(ComponentID))
            {
                default(TEdge).ModifyComponents(ref componentIDs, add);
            }
            else
            {
                default(TEdge).ModifyTags(ref tagIDs, add);
            }

            Archetype archetype = Archetype.CreateOrGetExistingArchetype(
                componentIDs.AsSpan(),
                tagIDs.AsSpan(),
                world,
                componentIDs,
                tagIDs);

            cache.Set(archetypeFromID.RawIndex, archetype.ID.RawIndex);

            return archetype;
        }

        return destination;
    }

    internal interface IArchetypeGraphEdge
    {
        void ModifyTags(ref ImmutableArray<TagID> tags, bool add);
        void ModifyComponents(ref ImmutableArray<ComponentID> components, bool add);
        int TypeCount { get; }
    }
}