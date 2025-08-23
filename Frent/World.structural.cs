using Frent.Collections;
using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

partial class World
{
    /*  
     *  This file contains all core functions related to structual changes on the world
     *  The only core structual change function not here is the normal create function, since it needs to be source generated
     *  These functions take all the data it needs, with no validation that an entity is alive
     */

    internal void RemoveArchetypicalComponent(Entity entity, ref EntityLocation lookup, ComponentID componentID)
    {
        Archetype destination = RemoveComponentLookup.FindAdjacentArchetypeID(componentID, lookup.ArchetypeID, this, ArchetypeEdgeType.RemoveComponent)
            .Archetype(this);

        MoveEntityToArchetypeRemove(entity, ref lookup, destination);
    }

    internal void AddArchetypicalComponent(Entity entity, ref EntityLocation lookup, ComponentID componentID, out EntityLocation entityLocation, out Archetype destination)
    {
        destination = AddComponentLookup.FindAdjacentArchetypeID(componentID, lookup.ArchetypeID, this, ArchetypeEdgeType.AddComponent)
            .Archetype(this);

        MoveEntityToArchetypeAdd(entity, ref lookup, out entityLocation, destination);
    }

    [SkipLocalsInit]
    internal void MoveEntityToArchetypeAdd(Entity entity, ref EntityLocation currentLookup, out EntityLocation nextLocation, Archetype destination)
    {
        Archetype from = currentLookup.Archetype;

        Debug.Assert(from.Components.Length < destination.Components.Length);

        destination.CreateEntityLocation(currentLookup.Flags, out nextLocation).Init(entity);
        nextLocation.Version = currentLookup.Version;

        Archetype.CopyBitset(from, destination, currentLookup.Index, nextLocation.Index);

        EntityIDOnly movedDown = from.DeleteEntityFromEntityArray(currentLookup.Index, out int deletedIndex);

        ComponentStorageRecord[] fromRunners = from.Components;
        ComponentStorageRecord[] destRunners = destination.Components;
        byte[] fromMap = from.ComponentTagTable;

        ImmutableArray<ComponentID> destinationComponents = destination.ArchetypeTypeArray;

        //int writeToIndex = 0;
        for (int i = 0; i < destinationComponents.Length;)
        {
            ComponentID componentToMove = destinationComponents[i];
            int fromIndex = fromMap.UnsafeArrayIndex(componentToMove.RawIndex) & GlobalWorldTables.IndexBits;

            //index for dest is offset by one for hardware trap
            i++;

            if (fromIndex == 0)
            {
                //writeTo.UnsafeSpanIndex(writeToIndex++) = destRunners[i];
            }
            else
            {
                destRunners.UnsafeArrayIndex(i).PullComponentFromAndClear(fromRunners.UnsafeArrayIndex(fromIndex).Buffer, nextLocation.Index, currentLookup.Index, deletedIndex);
            }
        }

        ref var displacedEntityLocation = ref EntityTable.UnsafeIndexNoResize(movedDown.ID);
        displacedEntityLocation.Archetype = currentLookup.Archetype;
        displacedEntityLocation.Index = currentLookup.Index;

        currentLookup.Archetype = nextLocation.Archetype;
        currentLookup.Index = nextLocation.Index;
    }

    /// <remarks>
    /// Does not handle events. Calls Destroy implicitly
    /// </remarks>>
    [SkipLocalsInit]
    internal void MoveEntityToArchetypeRemove(Entity entity, ref EntityLocation currentLookup, Archetype destination)
    {
        //NOTE: when moving EntityLocation between archetypes, version and flags cannot change
        Archetype from = currentLookup.Archetype;

        Debug.Assert(from.Components.Length > destination.Components.Length);

        destination.CreateEntityLocation(currentLookup.Flags, out var nextLocation).Init(entity);
        nextLocation.Version = currentLookup.Version;

        Archetype.CopyBitset(from, destination, currentLookup.Index, nextLocation.Index);

        EntityIDOnly movedDown = from.DeleteEntityFromEntityArray(currentLookup.Index, out int deletedIndex);

        ComponentStorageRecord[] fromRunners = from.Components;
        ComponentStorageRecord[] destRunners = destination.Components;
        byte[] destMap = destination.ComponentTagTable;

        ImmutableArray<ComponentID> fromComponents = from.ArchetypeTypeArray;

        DeleteComponentData deleteData = new DeleteComponentData(currentLookup.Index, deletedIndex);

        for (int i = 0; i < fromComponents.Length;)
        {
            // from -> to
            ComponentID componentToMove = fromComponents[i];
            int toIndex = destMap.UnsafeArrayIndex(componentToMove.RawIndex) & GlobalWorldTables.IndexBits;

            i++;

            if (toIndex == 0)
            {
                var runner = fromRunners.UnsafeArrayIndex(i);
                runner.Delete(deleteData);
            }
            else
            {
                destRunners.UnsafeArrayIndex(toIndex).PullComponentFromAndClear(fromRunners.UnsafeArrayIndex(i).Buffer, nextLocation.Index, currentLookup.Index, deletedIndex);
            }
        }

        //copy everything but 
        ref var displacedEntityLocation = ref EntityTable.UnsafeIndexNoResize(movedDown.ID);
        displacedEntityLocation.Archetype = currentLookup.Archetype;
        displacedEntityLocation.Index = currentLookup.Index;

        currentLookup.Archetype = nextLocation.Archetype;
        currentLookup.Index = nextLocation.Index;
    }

    [SkipLocalsInit]
    internal void MoveEntityToArchetypeIso(Entity entity, ref EntityLocation currentLookup, Archetype destination)
    {
        Archetype from = currentLookup.Archetype;

        Debug.Assert(from.Components.Length == destination.Components.Length);

        destination.CreateEntityLocation(currentLookup.Flags, out var nextLocation).Init(entity);
        nextLocation.Version = currentLookup.Version;

        Archetype.CopyBitset(from, destination, currentLookup.Index, nextLocation.Index);

        EntityIDOnly movedDown = from.DeleteEntityFromEntityArray(currentLookup.Index, out int deletedIndex);


        ComponentStorageRecord[] fromRunners = from.Components;
        ComponentStorageRecord[] destRunners = destination.Components;
        byte[] destMap = destination.ComponentTagTable;

        ImmutableArray<ComponentID> fromComponents = from.ArchetypeTypeArray;

        for (int i = 0; i < fromComponents.Length;)
        {
            int toIndex = destMap.UnsafeArrayIndex(fromComponents[i].RawIndex) & GlobalWorldTables.IndexBits;

            i++;

            destRunners[toIndex].PullComponentFromAndClear(fromRunners[i].Buffer, nextLocation.Index, currentLookup.Index, deletedIndex);
        }

        ref var displacedEntityLocation = ref EntityTable.UnsafeIndexNoResize(movedDown.ID);
        displacedEntityLocation.Archetype = currentLookup.Archetype;
        displacedEntityLocation.Index = currentLookup.Index;

        currentLookup.Archetype = nextLocation.Archetype;
        currentLookup.Index = nextLocation.Index;
    }

    #region Delete
    //Delete
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DeleteEntity(Entity entity, ref EntityLocation entityLocation)
    {
        EntityFlags check = entityLocation.Flags | WorldEventFlags;
        if ((check & EntityFlags.Events) != 0)
            InvokeDeleteEvents(entity, entityLocation);
        DeleteEntityWithoutEvents(entity, ref entityLocation);
    }

    //let the jit decide whether or not to inline
    private void InvokeDeleteEvents(Entity entity, EntityLocation entityLocation)
    {
        EntityDeletedEvent.Invoke(entity);
        if (entityLocation.HasFlag(EntityFlags.OnDelete))
        {
            foreach (var @event in EventLookup.GetValueRefOrNullRef(entity.EntityIDOnly).Delete.AsSpan())
            {
                @event.Invoke(entity);
            }
        }
        EventLookup.Remove(entity.EntityIDOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DeleteEntityWithoutEvents(Entity entity, ref EntityLocation currentLookup)
    {
        if (currentLookup.HasFlag(EntityFlags.HasSparseComponents))
            CleanupSparseComponents(entity, ref currentLookup);

        // entity is guaranteed to be alive here
        // entity is alive; Archetype is not null
        EntityIDOnly replacedEntity = currentLookup.Archetype!.DeleteEntity(currentLookup.Index);

        Debug.Assert(replacedEntity.ID < EntityTable._buffer.Length);
        Debug.Assert(entity.EntityID < EntityTable._buffer.Length);

        ref var replaced = ref EntityTable.UnsafeIndexNoResize(replacedEntity.ID);
        replaced.Index = currentLookup.Index;
        replaced.Archetype = currentLookup.Archetype;

        currentLookup.Archetype = null!;
        currentLookup.Version++;

        if (currentLookup.Version != ushort.MaxValue)
        {
            // don't let versions overflow
            // add entity to free list
            _freeListCount++;
            currentLookup.Index = _freelist;
            _freelist = entity.EntityID;
        }
    }

    internal void CleanupSparseComponents(Entity entity, ref EntityLocation currentLookup)
    {
        ref var bitset = ref currentLookup.GetBitset();

        Span<ComponentSparseSetBase> lookup = WorldSparseSetTable.AsSpan();
        foreach (int offset in bitset)
        {
            var set = lookup.UnsafeSpanIndex(offset);
            set.Remove(entity.EntityID, true);
        }
    }
    #endregion
}