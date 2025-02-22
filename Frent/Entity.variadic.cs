using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;

namespace Frent;

[Variadic("[null!]", "[|null!, |]", 8)]
[Variadic("            @event.Invoke(entity, Component<T>.ID);", "|            @event.Invoke(entity, Component<T$>.ID);\n|")]
[Variadic("            @event.Invoke(entity, Core.Tag<T>.ID);", "|            @event.Invoke(entity, Core.Tag<T$>.ID);\n|")]
[Variadic("        events.GenericEvent?.Invoke(entity, ref component);", "|        events.GenericEvent?.Invoke(entity, ref component$);\n|")]
[Variadic("        events.NormalEvent.Invoke(entity, Component<T>.ID);", "|        events.NormalEvent.Invoke(entity, Component<T$>.ID);\n|")]
[Variadic("ref T component", "|ref T$ component$, |")]
[Variadic("in T c1", "|in T$ c$, |")]
[Variadic("        ref var c1ref = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(buff.UnsafeSpanIndex(0))[nextLocation.Index]; c1ref = c1;", 
    "|        ref var c$ref = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T$>>(buff.UnsafeSpanIndex($ - 1))[nextLocation.Index]; c$ref = c$;\n|")]
[Variadic("Core.Tag<T>.ID", "[|Core.Tag<T$>.ID, |]")]
[Variadic("Component<T>.ID", "[|Component<T$>.ID, |]")]
[Variadic("<T>", "<|T$, |>")]
partial struct Entity
{
    //traversing archetype graph strategy:
    //1. hit small & fast static per type cache - 1 branch
    //2. dictionary lookup
    //3. create new archetype
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

        ref var c1ref = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(buff.UnsafeSpanIndex(0))[nextLocation.Index]; c1ref = c1;

        Component<T>.Initer?.Invoke(this, ref c1ref);

        EntityFlags flags = thisLookup.Location.Flags | world.WorldEventFlags;
        if(EntityLocation.HasEventFlag(flags, EntityFlags.AddComp | EntityFlags.WorldAddComp))
        {
            if(world.ComponentAddedEvent.HasListeners)
                InvokeComponentWorldEvents<T>(ref world.ComponentAddedEvent, this);

            if(EntityLocation.HasEventFlag(flags, EntityFlags.AddComp))
            {
                ref EventRecord eventRecord = ref CollectionsMarshal.GetValueRefOrNullRef(world.EventLookup, EntityIDOnly);
                InvokePerEntityEvents(this, ref eventRecord.Add, ref c1ref);
            }
        }
    }

    public void RemoveNew<T>()
    {
        ref EntityLookup thisLookup = ref AssertIsAlive(out World world);

        Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Remove.Lookup,
            ref thisLookup,
            false);

        Span<ComponentHandle> runners = stackalloc ComponentHandle[1];
        world.MoveEntityToArchetypeRemove(runners, this, ref thisLookup, out var nextLocation, to);
    }

    private static void InvokeComponentWorldEvents<T>(ref Event<ComponentID> @event, Entity entity)
    {
        @event.Invoke(entity, Component<T>.ID);
    }

    private static void InvokeTagWorldEvents<T>(ref TagEvent @event, Entity entity)
    {
        @event.Invoke(entity, Core.Tag<T>.ID);
    }

    private static void InvokePerEntityEvents<T>(Entity entity, ref ComponentEvent events, ref T component)
    {
        events.GenericEvent?.Invoke(entity, ref component);
        events.NormalEvent.Invoke(entity, Component<T>.ID);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        if (index == 32)
        {
            return NotInCache(world, ref cache, archetypeFromID, add);
        }
        else
        {
            return world.WorldArchetypeTable.UnsafeArrayIndex(cache.Lookup(index));
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
    }

    internal interface IArchetypeGraphEdge
    {
        void ModifyTags(ref ImmutableArray<TagID> tags, bool add);
        void ModifyComponents(ref ImmutableArray<ComponentID> components, bool add);
    }
}