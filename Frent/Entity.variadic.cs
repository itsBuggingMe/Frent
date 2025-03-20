using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

#if NETSTANDARD2_1
[Variadic("[null!]", "MemoryHelpers.SharedTempComponentStorageBuffer.AsSpan(0, $)")]
#else
[Variadic("[null!]", "[|null!, |]")]
#endif
[Variadic("        events.GenericEvent!.Invoke(entity, ref component);", "|        events.GenericEvent!.Invoke(entity, ref component$);\n|", 8)]
[Variadic("        events.NormalEvent.Invoke(entity, Component<T>.ID);", "|        events.NormalEvent.Invoke(entity, Component<T$>.ID);\n|")]
[Variadic("            world.WorldUpdateCommandBuffer.AddComponent(this, c1);", "|            world.WorldUpdateCommandBuffer.AddComponent(this, c$);\n|")]
[Variadic("            world.WorldUpdateCommandBuffer.RemoveComponent(this, Component<T>.ID);", "|            world.WorldUpdateCommandBuffer.RemoveComponent(this, Component<T$>.ID);\n|")]
[Variadic("ref T component", "|ref T$ component$, |")]
[Variadic("in T c1", "|in T$ c$, |")]
[Variadic("stackalloc ComponentHandle[1]", "stackalloc ComponentHandle[$]")]
[Variadic("        @event.InvokeInternal(entity, Component<T>.ID);", "|        @event.InvokeInternal(entity, Component<T$>.ID);\n|")]

[Variadic("        @event.InvokeInternal(entity, Core.Tag<T>.ID);", "|        @event.InvokeInternal(entity, Core.Tag<T$>.ID);\n|")]
[Variadic("        events.Invoke(entity, Core.Tag<T>.ID);", "|        events.Invoke(entity, Core.Tag<T$>.ID);\n|")]

[Variadic("        Component<T>.Initer?.Invoke(this, ref c1ref);", "|        Component<T$>.Initer?.Invoke(this, ref c$ref);\n|")]
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
    //3. find existing archetype
    //4. create new archetype

    /// <summary>
    /// Adds a component to this <see cref="Entity"/>.
    /// </summary>
    /// <remarks>If the world is being updated, changed are deffered to the end of the world update.</remarks>
    [SkipLocalsInit]
    public void Add<T>(in T c1)
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.AddComponent(this, c1);
            return;
        }

        Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Add.Lookup,
            ref thisLookup,
            true);

        Span<ComponentStorageBase> buff = [null!];
        world.MoveEntityToArchetypeAdd(buff, this, ref thisLookup, out EntityLocation nextLocation, to);

        ref var c1ref = ref UnsafeExtensions.UnsafeCast<ComponentStorage<T>>(buff.UnsafeSpanIndex(0))[nextLocation.Index]; c1ref = c1;

        Component<T>.Initer?.Invoke(this, ref c1ref);

        EntityFlags flags = thisLookup.Flags;
        if (EntityLocation.HasEventFlag(flags | world.WorldEventFlags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
        {
            if (world.ComponentAddedEvent.HasListeners)
                InvokeComponentWorldEvents<T>(ref world.ComponentAddedEvent, this);

            if (EntityLocation.HasEventFlag(flags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
            {
#if NETSTANDARD2_1
                EventRecord events = world.EventLookup[EntityIDOnly];
#else
                ref EventRecord events = ref CollectionsMarshal.GetValueRefOrNullRef(world.EventLookup, EntityIDOnly);
#endif
                InvokePerEntityEvents(this, EntityLocation.HasEventFlag(thisLookup.Flags, EntityFlags.AddGenericComp), ref events.Add, ref c1ref);
            }
        }
    }

    /// <summary>
    /// Removes a component from this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    [SkipLocalsInit]
    public void Remove<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

        if (!world.AllowStructualChanges)
        {
            world.WorldUpdateCommandBuffer.RemoveComponent(this, Component<T>.ID);
            return;
        }

        Archetype to = TraverseThroughCacheOrCreate<ComponentID, NeighborCache<T>>(
            world,
            ref NeighborCache<T>.Remove.Lookup,
            ref thisLookup,
            false);

        Span<ComponentHandle> runners = stackalloc ComponentHandle[1];
        world.MoveEntityToArchetypeRemove(runners, this, ref thisLookup, to);
        //world.MoveEntityToArchetypeRemove invokes the events for us
    }

    /// <summary>
    /// Adds a tag to this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    [SkipLocalsInit]
    public void Tag<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

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
#if NETSTANDARD2_1
                EventRecord events = world.EventLookup[EntityIDOnly];
#else
                ref EventRecord events = ref CollectionsMarshal.GetValueRefOrNullRef(world.EventLookup, EntityIDOnly);
#endif
                InvokePerEntityTagEvents<T>(this, ref events.Tag);
            }
        }
    }

    /// <summary>
    /// Removes a tag from this <see cref="Entity"/>
    /// </summary>
    /// <inheritdoc cref="Add{T}(in T)"/>
    [SkipLocalsInit]
    public void Detach<T>()
    {
        ref EntityLocation thisLookup = ref AssertIsAlive(out World world);

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
#if NETSTANDARD2_1
                EventRecord events = world.EventLookup[EntityIDOnly];
#else
                ref EventRecord events = ref CollectionsMarshal.GetValueRefOrNullRef(world.EventLookup, EntityIDOnly);
#endif
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
        public void ModifyTags(ref ImmutableArray<TagID> tags, bool add)
        {
            if (add)
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