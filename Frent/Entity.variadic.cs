using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

[Variadic(nameof(Entity))]
partial struct Entity
{
    // traversing archetype graph strategy:
    //1. hit small & fast static per type cache - 1 branch
    //2. dictionary lookup
    //3. find existing archetype
    //4. create new archetype

    /// <summary>
    /// Adds a component to this <see cref="Entity"/>.
    /// </summary>
    /// <remarks>If the world is being updated, changed are deffered to the end of the world update.</remarks>
    /// <variadic />
    [SkipLocalsInit]
    public void Add<T>(in T c1)
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.AddComponent(this, c1);
            return;
        }

        Unsafe.SkipInit(out int archIndex);
        MemoryHelpers.Poison(ref archIndex);
        Archetype? to = null;

        ref ComponentSparseSetBase sparseSets = ref NeighborCache<T>.HasAnySparseComponents ? 
            ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable) :
            ref Unsafe.NullRef<ComponentSparseSetBase>();

        if (NeighborCache<T>.HasAnyArchetypicalComponents)
        {
            to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
                world,
                ref NeighborCache<T>.Add.Lookup,
                ref thisLookup,
                true);

            world.MoveEntityToArchetypeAdd(this, ref thisLookup, out EntityLocation nextLocation, to);
            archIndex = nextLocation.Index;
        }

        ref var c1ref = ref Component<T>.IsSparseComponent ?
            ref MemoryHelpers.GetSparseSet<T>(ref sparseSets).AddComponent(EntityID) :
            ref to!.GetComponentStorage<T>().UnsafeIndex<T>(archIndex);
        c1ref = c1;

        if (NeighborCache<T>.HasAnySparseComponents)
        {
            thisLookup.Flags |= EntityFlags.HasSparseComponents;
            ref Bitset set = ref NeighborCache<T>.HasAnyArchetypicalComponents ?
                ref to!.GetBitset(archIndex) : // guarded by HasAnyArchetypicalComponents
                ref thisLookup.Archetype.GetBitset(thisLookup.Index);

            if(Component<T>.IsSparseComponent) set.Set(Component<T>.SparseSetComponentIndex);
        }

        Component<T>.Initer?.Invoke(this, ref c1ref);

        EntityFlags flags = thisLookup.Flags;
        if (EntityLocation.HasEventFlag(flags | world.WorldEventFlags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
        {
            if (world.ComponentAddedEvent.HasListeners)
                InvokeComponentWorldEvents<T>(ref world.ComponentAddedEvent, this);

            if (EntityLocation.HasEventFlag(flags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
            {
                ref EventRecord events = ref world.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                InvokePerEntityEvents(this, EntityLocation.HasEventFlag(thisLookup.Flags, EntityFlags.AddGenericComp), ref events.Add, ref c1ref);
            }
        }
    }

    /// <summary>
    /// Removes a component from this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    /// <variadic /> 
    [SkipLocalsInit]
    public void Remove<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.RemoveComponent(this, Component<T>.ID);
            return;
        }

        
        // get comp refs for events & destroyer calling
        ref ComponentSparseSetBase first = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);
        Archetype from = thisLookup.Archetype;
        ref T c1ref = ref Component<T>.Destroyer is null ?
            ref Unsafe.NullRef<T>() : 
            ref Component<T>.IsSparseComponent ? 
                ref MemoryHelpers.GetSparseSet<T>(ref first)[EntityID] : 
                ref Unsafe.Add(ref from.GetComponentDataReference<T>(), thisLookup.Index);


        if (EntityLocation.HasEventFlag(thisLookup.Flags | world.WorldEventFlags, EntityFlags.RemoveComp | EntityFlags.RemoveGenericComp))
        {
            if (world.ComponentRemovedEvent.HasListeners)
                InvokeComponentWorldEvents<T>(ref world.ComponentRemovedEvent, this);

            if (EntityLocation.HasEventFlag(thisLookup.Flags, EntityFlags.RemoveComp | EntityFlags.RemoveGenericComp))
            {
                // fill in the gaps
                if (Component<T>.Destroyer is null) c1ref = ref Component<T>.IsSparseComponent ?
                        ref MemoryHelpers.GetSparseSet<T>(ref first)[EntityID] :
                        ref Unsafe.Add(ref from.GetComponentDataReference<T>(), thisLookup.Index);

                ref var events = ref world.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                InvokePerEntityEvents(this, EntityLocation.HasEventFlag(thisLookup.Flags, EntityFlags.RemoveGenericComp), ref events.Remove, ref c1ref);
            }

        }

        // Call Destroyers
        Component<T>.Destroyer?.Invoke(ref c1ref);

        // Actually move components

        ref Bitset bits = ref Unsafe.NullRef<Bitset>();
        ref ComponentSparseSetBase start = ref Unsafe.NullRef<ComponentSparseSetBase>();
        if (NeighborCache<T>.HasAnySparseComponents)
        {
            bits = ref thisLookup.Archetype.GetBitset(thisLookup.Index);

            start = ref MemoryMarshal.GetArrayDataReference(world.WorldSparseSetTable);
        }

        // set sparse components and bits

        if (Component<T>.IsSparseComponent)
        {
            bits.ClearAt(Component<T>.SparseSetComponentIndex);
            UnsafeExtensions.UnsafeCast<ComponentSparseSet<T>>(Unsafe.Add(ref start, Component<T>.SparseSetComponentIndex)).Remove(EntityID, false);
        }

        if (NeighborCache<T>.HasAnyArchetypicalComponents)
        {   
            Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
                world,
                ref NeighborCache<T>.Remove.Lookup,
                ref thisLookup,
                false);

            world.MoveEntityToArchetypeRemove(this, ref thisLookup, to);
        }
    }

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    /// <variadic />
    [SkipLocalsInit]
    public void Tag<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if(!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Tag<T>(this);
            return;
        }

        Archetype to = TraverseThroughCacheOrCreate<TagID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Tag.Lookup,
            ref thisLookup,
            true);

        world.MoveEntityToArchetypeIso(this, ref thisLookup, to);

        EntityFlags flags = thisLookup.Flags | world.WorldEventFlags;
        if (EntityLocation.HasEventFlag(flags, EntityFlags.Tagged))
        {
            if (world.Tagged.HasListeners)
                InvokeTagWorldEvents<T>(ref world.Tagged, this);

            if (EntityLocation.HasEventFlag(flags, EntityFlags.Tagged))
            {
                ref EventRecord events = ref world.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                InvokePerEntityTagEvents<T>(this, ref events.Tag);
            }
        }
    }

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    /// <variadic />
    [SkipLocalsInit]
    public void Detach<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.Detach<T>(this);
            return;
        }

        Archetype to = TraverseThroughCacheOrCreate<TagID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Detach.Lookup,
            ref thisLookup,
            false);

        world.MoveEntityToArchetypeIso(this, ref thisLookup, to);

        EntityFlags flags = thisLookup.Flags | world.WorldEventFlags;
        if (EntityLocation.HasEventFlag(flags, EntityFlags.Detach))
        {
            if (world.Detached.HasListeners)
                InvokeTagWorldEvents<T>(ref world.Detached, this);

            if (EntityLocation.HasEventFlag(flags, EntityFlags.Detach))
            {
                ref EventRecord events = ref world.EventLookup.GetValueRefOrNullRef(EntityIDOnly);
                InvokePerEntityTagEvents<T>(this, ref events.Detach);
            }
        }
    }

    private static void InvokeComponentWorldEvents<T>(ref Event<ComponentID> @event, Entity entity)
    {
        @event.InvokeInternal(entity, Component<T>.ID);
    }

    private static void InvokePerEntityEvents<T>(Entity entity, bool hasGenericEvent, ref ComponentEvent events, ref T component)
    {
        events.NormalEvent.Invoke(entity, Component<T>.ID);

        if (!hasGenericEvent)
            return;

        events.GenericEvent!.Invoke(entity, ref component);
    }

    private static void InvokeTagWorldEvents<T>(ref TagEvent @event, Entity entity)
    {
        @event.InvokeInternal(entity, Core.Tag<T>.ID);
    }

    private static void InvokePerEntityTagEvents<T>(Entity entity, ref TagEvent events)
    {
        events.Invoke(entity, Core.Tag<T>.ID);
    }

    private struct NeighborCache<T> : IArchetypeGraphEdge
    {
        public static bool HasAnyArchetypicalComponents => !Component<T>.IsSparseComponent;
        public static bool HasAnySparseComponents => Component<T>.IsSparseComponent;

        public void WriteComponentIDs(ref Span<ComponentID> ids)
        {
            //id.Length == 8
            ids.UnsafeSpanIndex(0) = Component<T>.ID;
            ids = ids.Slice(0, 1);
        }

        public void WriteTagIDs(ref Span<TagID> ids)
        {
            //id.Length == 8
            ids.UnsafeSpanIndex(0) = Core.Tag<T>.ID;
            ids = ids.Slice(0, 1);
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M() { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Archetype TraverseThroughCacheOrCreate<T, TEdge>(
        World world,
        ref ArchetypeNeighborCache cache,
        ref EntityLocation currentLookup,
        bool add)
            where T : ITypeID
            where TEdge : struct, IArchetypeGraphEdge
    {
        ArchetypeID archetypeFromID = currentLookup.ArchetypeID;
        int index = cache.Traverse(archetypeFromID.RawIndex);

        if (index == 32)
        {
            return NotInCache(world, ref cache, archetypeFromID, add);
        }
        else
        {
            return Archetype.CreateOrGetExistingArchetype(new EntityType(cache.Lookup(index)), world);
        }

        [SkipLocalsInit]
        static Archetype NotInCache(World world, ref ArchetypeNeighborCache cache, ArchetypeID archetypeFromID, bool add)
        {
            ImmutableArray<ComponentID> componentIDs = archetypeFromID.Types;
            ImmutableArray<TagID> tagIDs = archetypeFromID.Tags;

            if (typeof(T) == typeof(ComponentID))
            {
                Span<ComponentID> componentsSpecified = stackalloc ComponentID[8];
                default(TEdge).WriteComponentIDs(ref componentsSpecified);
                Span<ComponentID> archetypicals = stackalloc ComponentID[8];

                int index = 0;
                foreach (var maybeDelta in componentsSpecified)
                    if (!maybeDelta.IsSparseComponent)
                        archetypicals[index++] = maybeDelta;
                archetypicals = archetypicals.Slice(0, index);

                componentIDs = add ? MemoryHelpers.Concat(componentIDs, archetypicals)
                    : MemoryHelpers.Remove(componentIDs, archetypicals);
            }
            else
            {
                Span<TagID> delta = stackalloc TagID[8];
                default(TEdge).WriteTagIDs(ref delta);
                tagIDs = add ? MemoryHelpers.Concat(tagIDs, delta) 
                    : MemoryHelpers.Remove(tagIDs, delta);
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
        void WriteComponentIDs(ref Span<ComponentID> ids);
        void WriteTagIDs(ref Span<TagID> ids);
    }
}