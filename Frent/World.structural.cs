using Frent.Collections;
using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
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
    /// <summary>
    /// Note - This function DOES NOT invoke events, as it is also used for command buffer entity creation
    /// </summary>
    internal void AddComponentRange(Entity entity, ReadOnlySpan<ComponentHandle> comps)
    {
        ref EntityLocation location = ref EntityTable.UnsafeIndexNoResize(entity.EntityID).Location;

        Archetype currentArchetype = location.Archetype;

        ReadOnlySpan<ComponentID> existingComponentIDs = currentArchetype.ArchetypeTypeArray.AsSpan();
        int newCompCount = comps.Length + existingComponentIDs.Length;
        if (newCompCount > MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_InvalidOperationException("Too many components");

        Span<ComponentID> allComps = stackalloc ComponentID[newCompCount];
        existingComponentIDs.CopyTo(allComps);
        int j = 0;
        for (int i = existingComponentIDs.Length; i < comps.Length; i++)
            allComps[i] = comps[j++].ComponentID;
        var tags = currentArchetype.ArchetypeTagArray;

        ArchetypeID destinationID = currentArchetype.ID;
        foreach (var component in comps)
        {
            destinationID = CompAddLookup.FindAdjacentArchetypeID(component.ComponentID, destinationID, Archetype.ArchetypeStructualAction.Add, this);
        }
        Archetype destination = destinationID.Archetype(this);

        destination.CreateEntityLocation(location.Flags, out var nextELoc).Init(entity);

        for (int i = 1; i < currentArchetype.Components.Length; i++)
        {
            destination.Components[i].PullComponentFromAndClear(currentArchetype.Components[i], nextELoc.Index, location.Index);
        }

        j = 0;
        for (int i = existingComponentIDs.Length; i < currentArchetype.Components.Length - 1; i++)
        {
            var componentLocation = comps[j++];
            currentArchetype.Components[i].PullComponentFrom(
                Component.ComponentTable[componentLocation.ComponentID.RawIndex].Storage,
                nextELoc.Index,
                componentLocation.Index);
        }


        EntityIDOnly movedDown = currentArchetype.DeleteEntityFromStorage(location.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = location;
        location = nextELoc;
    }

    //Add
    //Note: this fucntion doesn't actually do the last step of setting the component in the new archetype
    //the caller's job is to set the component
    [SkipLocalsInit]
    internal IComponentRunner AddComponent(EntityIDOnly entity, ref EntityLookup currentLookup, ComponentID component, out EntityLocation nextLocation)
    {
        Archetype from = currentLookup.Location.Archetype;

        Archetype destination = CompAddLookup.FindAdjacentArchetype(component, from, Archetype.ArchetypeStructualAction.Add, this);

        destination.CreateEntityLocation(currentLookup.Location.Flags, out nextLocation).Init(entity);

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(currentLookup.Location.Index);

        IComponentRunner[] fromRunners = from.Components;
        IComponentRunner[] toRunners = destination.Components;

        int i = 1;
        for (; i < fromRunners.Length; i++)
        {
            toRunners[i].PullComponentFromAndClear(fromRunners[i], nextLocation.Index, currentLookup.Location.Index);
        }

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = currentLookup.Location;
        currentLookup.Location = nextLocation;

        return toRunners.UnsafeArrayIndex(i);
    }

    //Remove
    internal void RemoveComponent(Entity entity, ref EntityLookup currentLookup, ComponentID component)
    {
        Archetype destination = CompAddLookup.FindAdjacentArchetype(component, currentLookup.Location.Archetype, Archetype.ArchetypeStructualAction.Remove, this);

        ref EntityIDOnly archetypeEntity = ref destination.CreateEntityLocation(currentLookup.Location.Flags, out EntityLocation nextLocation);
        archetypeEntity.Init(entity);


        int skipIndex = currentLookup.Location.Archetype.GetComponentIndex(component);

        IDTable? tmpEventComponentStorage = null;
        int tmpEventComponentIndex = -1;

        ref IComponentRunner destRef = ref MemoryMarshal.GetArrayDataReference(destination.Components);
        ref IComponentRunner fromRef = ref MemoryMarshal.GetArrayDataReference(currentLookup.Location.Archetype.Components);
        destRef = ref Unsafe.Add(ref destRef, 1);
        fromRef = ref Unsafe.Add(ref fromRef, 1);

        int i = 1;

        for (; i < skipIndex; i++)
        {
            destRef.PullComponentFromAndClear(fromRef, nextLocation.Index, currentLookup.Location.Index);
            destRef = ref Unsafe.Add(ref destRef, 1);
            fromRef = ref Unsafe.Add(ref fromRef, 1);
        }

        if (currentLookup.Location.HasEvent(EntityFlags.RemoveComp))
        {
            tmpEventComponentStorage = fromRef.PushComponentToStack(currentLookup.Location.Index, out tmpEventComponentIndex);
        }

        for (i++, fromRef = ref Unsafe.Add(ref fromRef, 1); i < currentLookup.Location.Archetype.Components.Length; i++)
        {
            destRef.PullComponentFromAndClear(fromRef, nextLocation.Index, currentLookup.Location.Index);
            destRef = ref Unsafe.Add(ref destRef, 1);
            fromRef = ref Unsafe.Add(ref fromRef, 1);
        }

        EntityIDOnly movedDown = currentLookup.Location.Archetype.DeleteEntityFromStorage(currentLookup.Location.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = currentLookup.Location;
        currentLookup.Location = nextLocation;

        currentLookup.Location.Flags |= WorldEventFlags;
        if(currentLookup.Location.HasEvent(EntityFlags.RemoveComp | EntityFlags.WorldRemoveComp))
        {
            ComponentRemovedEvent.Invoke(entity, component);
            ref var eventData = ref CollectionsMarshal.GetValueRefOrNullRef(EventLookup, entity.EntityIDOnly);
            eventData.Remove.NormalEvent.Invoke(entity, component);
            tmpEventComponentStorage?.InvokeEventWithAndConsume(eventData.Remove.GenericEvent, entity, tmpEventComponentIndex);
        }
    }

    //components cannot be empty
    [SkipLocalsInit]
    internal void RemoveComponentRange(Entity entity, EntityLocation entityLocation, ReadOnlySpan<ComponentID> components)
    {
        Debug.Assert(components.Length != 0);
        Archetype from = entityLocation.Archetype;

        ArchetypeID destinationID = from.ID;
        foreach(var component in components)
        {
            destinationID = CompAddLookup.FindAdjacentArchetypeID(component, destinationID, Archetype.ArchetypeStructualAction.Remove, this);
        }
        Archetype destination = destinationID.Archetype(this);

        destination!.CreateEntityLocation(entityLocation.Flags, out EntityLocation nextLocation).Init(entity);

        Span<int> skipIndicies = stackalloc int[components.Length];
        Span<int> stackIndicies = stackalloc int[components.Length];
        Span<IDTable> stacks = [null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!];
        int skipIndexIndex = 0;

        for(int i1 = 0; i1 < skipIndicies.Length; i1++)
        {
            skipIndicies[i1] = from.GetComponentIndex(components[i1]);
        }
        skipIndicies.Sort();

        int j = 1;
        for(int i = 1; i < from.Components.Length; i++)
        {
            if(i == skipIndicies[skipIndexIndex])
            {
                if(entityLocation.HasEvent(EntityFlags.AddComp))
                {
                    stacks[skipIndexIndex] = from.Components[i].PushComponentToStack(entityLocation.Index, out int stackIndex);
                    stackIndicies[skipIndexIndex] = stackIndex;
                }
                skipIndexIndex++;
                continue;
            }

            destination.Components[j++].PullComponentFromAndClear(from.Components[i], nextLocation.Index, entityLocation.Index);
        }

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(entityLocation.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = entityLocation;
        EntityTable.UnsafeIndexNoResize(entity.EntityID).Location = nextLocation;

        entityLocation.Flags |= WorldEventFlags;
        if (entityLocation.HasEvent(EntityFlags.RemoveComp | EntityFlags.WorldRemoveComp))
        {
            for(int i = 0; i < components.Length; i++)
            {
                ComponentRemovedEvent.Invoke(entity, components[i]);
                ref var eventData = ref CollectionsMarshal.GetValueRefOrNullRef(EventLookup, entity.EntityIDOnly);
                eventData.Remove.NormalEvent.Invoke(entity, components[i]);
                stacks[i]?.InvokeEventWithAndConsume(eventData.Remove.GenericEvent, entity, stackIndicies[i]);
            }
        }
    }

    //Delete
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DeleteEntity(Entity entity, ref EntityLookup entityLocation)
    {
        EntityFlags check = entityLocation.Location.Flags | WorldEventFlags;
        if ((check & EntityFlags.AllEvents) != 0)
            InvokeDeleteEvents(entity, entityLocation.Location);
        DeleteEntityWithoutEvents(entity, ref entityLocation);
    }

    //let the jit decide whether or not to inline
    private void InvokeDeleteEvents(Entity entity, EntityLocation entityLocation)
    {
        EntityDeletedEvent.Invoke(entity);
        if (entityLocation.HasEvent(EntityFlags.OnDelete))
        {
            foreach (var @event in EventLookup[entity.EntityIDOnly].Delete.AsSpan())
            {
                @event.Invoke(entity);
            }
        }
        EventLookup.Remove(entity.EntityIDOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DeleteEntityWithoutEvents(Entity entity, ref EntityLookup currentLookup)
    {
        //entity is guaranteed to be alive here
        EntityIDOnly replacedEntity = currentLookup.Location.Archetype.DeleteEntity(currentLookup.Location.Index);

        Debug.Assert(replacedEntity.ID < EntityTable._buffer.Length);
        Debug.Assert(entity.EntityID < EntityTable._buffer.Length);

        ref var replaced = ref EntityTable.UnsafeIndexNoResize(replacedEntity.ID);
        replaced.Location = currentLookup.Location;
        replaced.Version = replacedEntity.Version;
        currentLookup.Version = ushort.MaxValue;

        if (entity.EntityVersion != ushort.MaxValue - 1)
        {
            //can't use max value as an ID, as it is used as a default value
            ref var id = ref RecycledEntityIds.Push();
            id = entity.EntityIDOnly;
            id.Version++;
        }
    }

    //Tag
    internal bool Tag(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        if (entityLocation.Archetype.HasTag(tagID))
            return false;

        Archetype from = entityLocation.Archetype;

        ref var destination = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Tag(tagID, entityLocation.ArchetypeID, ArchetypeEdgeType.AddTag),
            out bool exist);

        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), MemoryHelpers.Concat(from.ArchetypeTagArray, tagID, out var res), this, from.ArchetypeTypeArray, res);
        }

        destination!.CreateEntityLocation(entityLocation.Flags, out var nextLocation).Init(entity);

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 1; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFromAndClear(fromRunners[i], nextLocation.Index, entityLocation.Index);

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(entityLocation.Index);

        EntityTable[movedDown.ID].Location = entityLocation;
        EntityTable[entity.EntityID].Location = nextLocation;

        ref var eventData = ref TryGetEventData(entityLocation, entity.EntityIDOnly, EntityFlags.Tagged, out bool eventExist);
        if (eventExist)
            eventData.Tag.Invoke(entity, tagID);

        Tagged.Invoke(entity, tagID);

        return true;
    }

    //Detach
    internal bool Detach(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        if (!GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID))
            return false;

        Archetype from = entityLocation.Archetype;
        ref var destination = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Tag(tagID, from.ID, ArchetypeEdgeType.RemoveTag),
            out bool exist);

        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), MemoryHelpers.Remove(from.ArchetypeTagArray, tagID, out var arr), this, from.ArchetypeTypeArray, arr);
        }

        destination!.CreateEntityLocation(entityLocation.Flags, out var nextLocation).Init(entity);

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 1; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFromAndClear(fromRunners[i], nextLocation.Index, entityLocation.Index);

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(entityLocation.Index);
        
        EntityTable[movedDown.ID].Location = entityLocation;
        EntityTable[entity.EntityID].Location = nextLocation;


        ref var eventData = ref TryGetEventData(entityLocation, entity.EntityIDOnly, EntityFlags.Detach, out bool eventExist);
        if (eventExist)
            eventData.Detach.Invoke(entity, tagID);

        Detached.Invoke(entity, tagID);

        return true;
    }
}