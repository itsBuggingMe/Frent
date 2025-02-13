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
    internal void AddComponentRange(Entity entity, ReadOnlySpan<(ComponentID Component, int Index)> comps)
    {
        EntityLocation location = EntityTable[entity.EntityID].Location;
        Archetype currentArchetype = location.Archetype;

        ReadOnlySpan<ComponentID> existingComponentIDs = currentArchetype.ArchetypeTypeArray.AsSpan();
        int newCompCount = comps.Length + existingComponentIDs.Length;
        if ((uint)newCompCount > 16)
            FrentExceptions.Throw_InvalidOperationException("Too many components");

        Span<ComponentID> allComps = stackalloc ComponentID[newCompCount];
        existingComponentIDs.CopyTo(allComps);
        int j = 0;
        for (int i = existingComponentIDs.Length; i < comps.Length; i++)
            allComps[i] = comps[j++].Component;
        var tags = currentArchetype.ArchetypeTagArray;

        var destination = Archetype.CreateOrGetExistingArchetype(allComps, tags.AsSpan(), this, null, tags);
        destination.CreateEntityLocation(location.Flags, out var nextELoc).Init(entity);

        for (int i = 1; i < currentArchetype.Components.Length; i++)
        {
            destination.Components[i].PullComponentFromAndDelete(currentArchetype.Components[i], nextELoc.Index, location.Index);
        }

        j = 0;
        for (int i = existingComponentIDs.Length; i < currentArchetype.Components.Length - 1; i++)
        {
            var componentLocation = comps[j++];
            currentArchetype.Components[i].PullComponentFrom(
                Component.ComponentTable[componentLocation.Component.ID].Stack,
                nextELoc.Index,
                componentLocation.Index);
        }


        EntityIDOnly movedDown = currentArchetype.DeleteEntityFromStorage(location.Index);

        EntityTable[movedDown.ID].Location = location;
        EntityTable[entity.EntityID].Location = nextELoc;
    }

    //Add
    //Note: this fucntion doesn't actually do the last step of setting the component in the new archetype
    //the caller's job is to set the component
    [SkipLocalsInit]
    internal IComponentRunner AddComponent(EntityIDOnly entity, EntityLocation entityLocation, ComponentID component, out EntityLocation nextLocation)
    {
        Archetype from = entityLocation.Archetype;

        Archetype? destination;
        uint key = CompAddLookup.GetKey(component.ID, entityLocation.ArchetypeID);
        int index = CompAddLookup.LookupIndex(key);
        if(index != 32)
        {
            destination = CompAddLookup.Archetypes.UnsafeArrayIndex(index);
        }
        else if(!CompAddLookup.FallbackLookup.TryGetValue(key, out destination))
        {
            destination = from.FindArchetypeAdjacentAdd(this, component);
        }

        destination.CreateEntityLocation(entityLocation.Flags, out nextLocation).Init(entity);

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(entityLocation.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = entityLocation;
        EntityTable.UnsafeIndexNoResize(entity.ID).Location = nextLocation;

        IComponentRunner[] fromRunners = from.Components;
        IComponentRunner[] toRunners = destination.Components;

        int i = 1;
        for (; i < fromRunners.Length; i++)
        {
            toRunners[i].PullComponentFromAndDelete(fromRunners[i], nextLocation.Index, entityLocation.Index);
        }

        return toRunners.UnsafeArrayIndex(i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T i)
    {

    }

    //Remove
    internal void RemoveComponent(Entity entity, EntityLocation entityLocation, ComponentID component)
    {
        Archetype? destination;
        uint key = CompRemoveLookup.GetKey(component.ID, entityLocation.ArchetypeID);
        int index = CompRemoveLookup.LookupIndex(key);

        if (index != 32)
        {
            destination = CompRemoveLookup.Archetypes.UnsafeArrayIndex(index);
        }
        else
        {
            destination = GetArchetypeRemoveNoCache(key, entityLocation.Archetype, component);
        }

        ref EntityIDOnly archetypeEntity = ref destination.CreateEntityLocation(entityLocation.Flags, out EntityLocation nextLocation);
        archetypeEntity.Init(entity);


        int skipIndex = entityLocation.Archetype.ComponentTagTable.UnsafeArrayIndex(component.ID);

        TrimmableStack? tmpEventComponentStorage = null;
        int tmpEventComponentIndex = -1;

        ref IComponentRunner destRef = ref MemoryMarshal.GetArrayDataReference(destination.Components);
        ref IComponentRunner fromRef = ref MemoryMarshal.GetArrayDataReference(entityLocation.Archetype.Components);
        destRef = ref Unsafe.Add(ref destRef, 1);
        fromRef = ref Unsafe.Add(ref fromRef, 1);

        int i = 1;

        for (; i < skipIndex; i++)
        {
            destRef.PullComponentFromAndDelete(fromRef, nextLocation.Index, entityLocation.Index);
            destRef = ref Unsafe.Add(ref destRef, 1);
            fromRef = ref Unsafe.Add(ref fromRef, 1);
        }

        if (entityLocation.HasEvent(EntityFlags.GenericRemoveComp))
        {
            fromRef.PushComponentToStack(entityLocation.Index, out tmpEventComponentIndex);
        }

        for (i++, fromRef = ref Unsafe.Add(ref fromRef, 1); i < entityLocation.Archetype.Components.Length; i++)
        {
            destRef.PullComponentFromAndDelete(fromRef, nextLocation.Index, entityLocation.Index);
            destRef = ref Unsafe.Add(ref destRef, 1);
            fromRef = ref Unsafe.Add(ref fromRef, 1);
        }

        EntityIDOnly movedDown = entityLocation.Archetype.DeleteEntityFromStorage(entityLocation.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = entityLocation;
        EntityTable.UnsafeIndexNoResize(entity.EntityID).Location = nextLocation;

        entityLocation.Flags |= WorldEventFlags;
        if(entityLocation.HasEvent(EntityFlags.RemoveComp | EntityFlags.GenericRemoveComp | EntityFlags.WorldRemoveComp))
        {
            ComponentRemovedEvent.Invoke(entity, component);
            ref var eventData = ref CollectionsMarshal.GetValueRefOrNullRef(EventLookup, entity.EntityIDOnly);
            eventData.Remove.NormalEvent.Invoke(entity, component);
            tmpEventComponentStorage?.InvokeEventWith(eventData.Remove.GenericEvent, entity, tmpEventComponentIndex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Archetype GetArchetypeRemoveNoCache(uint key, Archetype archetype, ComponentID component)
    {
        if (!CompRemoveLookup.FallbackLookup.TryGetValue(key, out Archetype? destination))
        {
            destination = archetype.FindArchetypeAdjacentRemove(this, component);
        }
        return destination;
    }

    //components cannot be empty
    [SkipLocalsInit]
    internal void RemoveComponents(Entity entity, EntityLocation entityLocation, ReadOnlySpan<ComponentID> components)
    {
        Debug.Assert(components.Length != 0);
        Archetype from = entityLocation.Archetype;

        Archetype destination = from;
        foreach(var component in components)
        {
            uint key = CompRemoveLookup.GetKey(component.ID, destination.ID);
            int index = CompRemoveLookup.LookupIndex(key);
            if (index != 32)
            {
                destination = CompRemoveLookup.Archetypes.UnsafeArrayIndex(index);
            }
            else if(CompRemoveLookup.FallbackLookup.TryGetValue(key, out Archetype? newDestination))
            {
                destination = newDestination;
            }
            else
            {
                destination = destination.FindArchetypeAdjacentRemove(this, component);
            }
        }

        destination!.CreateEntityLocation(entityLocation.Flags, out EntityLocation nextLocation).Init(entity);

        Span<int> skipIndicies = (stackalloc int[16])[..components.Length];
        Span<int> stackIndicies = (stackalloc int[16])[..components.Length];
        Span<TrimmableStack> stacks = [null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!];
        int skipIndexIndex = 0;

        for(int i1 = 0; i1 < skipIndicies.Length; i1++)
        {
            skipIndicies[i1] = from.ComponentTagTable[components[i1].ID];
        }
        skipIndicies.Sort();

        int j = 0;
        for(int i = 0; i < from.Components.Length; i++)
        {
            if(i == skipIndicies[skipIndexIndex])
            {
                if(entityLocation.HasEvent(EntityFlags.GenericAddComp))
                {
                    stacks[skipIndexIndex] = from.Components[i].PushComponentToStack(entityLocation.Index, out int stackIndex);
                    stackIndicies[skipIndexIndex] = stackIndex;
                }
                skipIndexIndex++;
                continue;
            }

            destination.Components[j++].PullComponentFromAndDelete(from.Components[i], nextLocation.Index, entityLocation.Index);
        }

        EntityIDOnly movedDown = from.DeleteEntityFromStorage(entityLocation.Index);

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = entityLocation;
        EntityTable.UnsafeIndexNoResize(entity.EntityID).Location = nextLocation;

        entityLocation.Flags |= WorldEventFlags;
        if (entityLocation.HasEvent(EntityFlags.RemoveComp | EntityFlags.GenericRemoveComp | EntityFlags.WorldRemoveComp))
        {
            for(int i = 0; i < components.Length; i++)
            {
                ComponentRemovedEvent.Invoke(entity, components[i]);
                ref var eventData = ref CollectionsMarshal.GetValueRefOrNullRef(EventLookup, entity.EntityIDOnly);
                eventData.Remove.NormalEvent.Invoke(entity, components[i]);
                stacks[i]?.InvokeEventWith(eventData.Remove.GenericEvent, entity, stackIndicies[i]);
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
            ref var id = ref _recycledEntityIds.Push();
            id = entity.EntityIDOnly;
            id.Version++;
        }
    }

    //Tag
    internal bool Tag(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        if (GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID))
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
            toRunners[i].PullComponentFromAndDelete(fromRunners[i], nextLocation.Index, entityLocation.Index);

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
            toRunners[i].PullComponentFromAndDelete(fromRunners[i], nextLocation.Index, entityLocation.Index);

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