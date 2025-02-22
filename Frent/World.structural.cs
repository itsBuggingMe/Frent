using Frent.Collections;
using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        throw new NotImplementedException();
    }

    //Add
    //Note: this function doesn't actually do the last step of setting the component in the new archetype
    //the caller's job is to set the component
    [SkipLocalsInit]
    internal IComponentRunner AddComponent(EntityIDOnly entity, ref EntityLookup currentLookup, ComponentID component, out EntityLocation nextLocation)
    {
        throw new NotImplementedException();
    }


    //components cannot be empty
    [SkipLocalsInit]
    internal void RemoveComponentRange(Entity entity, EntityLocation entityLocation, ReadOnlySpan<ComponentID> components)
    {
        throw new NotImplementedException();
    }

    //Tag
    internal bool Tag(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        throw new NotImplementedException();
    }

    //Detach
    internal bool Detach(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        throw new NotImplementedException();
    }

    [SkipLocalsInit]
    internal void MoveEntityToArchetypeAdd(Span<IComponentRunner> writeTo, Entity entity, ref EntityLookup currentLookup, out EntityLocation nextLocation, Archetype destination)
    {
        Archetype from = currentLookup.Location.Archetype;

        Debug.Assert(from.Components.Length < destination.Components.Length);

        destination.CreateEntityLocation(currentLookup.Location.Flags, out nextLocation).Init(entity);
        EntityIDOnly movedDown = from.DeleteEntityFromStorage(currentLookup.Location.Index, out int deletedIndex);


        IComponentRunner[] fromRunners = from.Components;
        IComponentRunner[] destRunners = destination.Components;
        byte[] fromMap = from.ComponentTagTable;

        ImmutableArray<ComponentID> destinationComponents = destination.ArchetypeTypeArray;

        int writeToIndex = 0;
        for(int i = 0; i < destinationComponents.Length;)
        {
            ComponentID componentToMove = destinationComponents[i];
            int fromIndex = fromMap.UnsafeArrayIndex(componentToMove.RawIndex) & GlobalWorldTables.IndexBits;

            //index for dest is offset by one for hardware trap
            i++;

            if(fromIndex == 0)
            {
                writeTo[writeToIndex++] = destRunners[i];
            }
            else
            {
                destRunners[i].PullComponentFromAndClear(fromRunners[fromIndex], nextLocation.Index, currentLookup.Location.Index, deletedIndex);
            }
        }

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = currentLookup.Location;
        currentLookup.Location = nextLocation;
    }

    [SkipLocalsInit]
    internal void MoveEntityToArchetypeRemove(Span<IComponentRunner> writeTo, Entity entity, ref EntityLookup currentLookup, out EntityLocation nextLocation, Archetype destination)
    {
        Archetype from = currentLookup.Location.Archetype;

        Debug.Assert(from.Components.Length > destination.Components.Length);

        destination.CreateEntityLocation(currentLookup.Location.Flags, out nextLocation).Init(entity);
        EntityIDOnly movedDown = from.DeleteEntityFromStorage(currentLookup.Location.Index, out int deletedIndex);


        IComponentRunner[] fromRunners = from.Components;
        IComponentRunner[] destRunners = destination.Components;
        byte[] destMap = destination.ComponentTagTable;

        ImmutableArray<ComponentID> fromComponents = from.ArchetypeTypeArray;

        int writeToIndex = 0;
        for (int i = 0; i < fromComponents.Length;)
        {
            ComponentID componentToMoveFromFromToTo = fromComponents[i];
            int toIndex = destMap.UnsafeArrayIndex(componentToMoveFromFromToTo.RawIndex);

            i++;

            if(toIndex == 0)
            {
                writeTo[writeToIndex++] = fromRunners[i];
            }
            else
            {
                destRunners[toIndex].PullComponentFromAndClear(fromRunners[i], nextLocation.Index, currentLookup.Location.Index, deletedIndex);
            }
        }

        EntityTable.UnsafeIndexNoResize(movedDown.ID).Location = currentLookup.Location;
        currentLookup.Location = nextLocation;


        //TODO: invoke events 
    }

    #region Delete
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
    #endregion
}