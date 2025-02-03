using Frent.Collections;
using Frent.Core;
using Frent.Core.Structures;
using Frent.Updating;
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
    /// <summary>
    /// Note - This function DOES NOT invoke events, as it is also used for command buffer entity creation
    /// </summary>
    internal void AddComponentRange(Entity entity, ReadOnlySpan<(ComponentID Component, int Index)> comps)
    {
        EntityLocation location = EntityTable[(uint)entity.EntityID].Location;
        Archetype currentArchetype = location.Archetype(this);

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
        destination.CreateEntityLocation(location.Flags, out var nextELoc) = entity;

        for (int i = 0; i < currentArchetype.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(currentArchetype.Components[i], nextELoc, location);
        }

        j = 0;
        for (int i = existingComponentIDs.Length; i < currentArchetype.Components.Length; i++)
        {
            var componentLocation = comps[j++];
            currentArchetype.Components[i].PullComponentFrom(
                Component.ComponentTable[componentLocation.Component.ID].Stack,
                nextELoc,
                componentLocation.Index);
        }


        Entity movedDown = currentArchetype.DeleteEntity(location.ChunkIndex, location.ComponentIndex);

        EntityTable.IndexWithInt(movedDown.EntityID).Location = location;
        EntityTable.IndexWithInt(entity.EntityID).Location = nextELoc;
    }

    //Add
    //Note: this fucntion doesn't actually do the last step of setting the component in the new archetype
    //the caller's job is to set the component
    internal Archetype AddComponent(Entity entity, EntityLocation entityLocation, ComponentID component, out IComponentRunner runner, out EntityLocation nextLocation)
    {
        Archetype from = entityLocation.Archetype(this);

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Component(component, entityLocation.ArchetypeID, ArchetypeEdgeType.AddTag),
            out bool exist);

        Archetype destination;

        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(Concat(from.ArchetypeTypeArray, component, out var res), from.ArchetypeTagArray.AsSpan(), this, res, from.ArchetypeTagArray);
            edge = destination.ID;
        }
        else
        {
            destination = WorldArchetypeTable[edge.ID];
        }

        destination.CreateEntityLocation(entityLocation.Flags, out nextLocation) = entity;

        for (int i = 0; i < from.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        runner = destination.Components[^1];

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable.GetValueNoCheck(movedDown.EntityID).Location = entityLocation;
        EntityTable.GetValueNoCheck(entity.EntityID).Location = nextLocation;

        _componentAdded.Invoke(entity, component);

        return destination;
    }

    //Remove
    internal void RemoveComponent(Entity entity, EntityLocation entityLocation, ComponentID component)
    {
        Archetype from = entityLocation.Archetype(this);

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Component(component, entityLocation.ArchetypeID, ArchetypeEdgeType.RemoveTag),
            out bool exist);


        Archetype destination;

        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(Remove(from.ArchetypeTypeArray, component, out var arr), from.ArchetypeTagArray.AsSpan(), this, arr, from.ArchetypeTagArray);
            edge = destination.ID;
        }
        else
        {
            destination = WorldArchetypeTable[edge.ID];
        }

        destination.CreateEntityLocation(entityLocation.Flags, out EntityLocation nextLocation) = entity;

        int skipIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, component);

        if (skipIndex >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException($"This entity doesn't have a component of type {component.Type.Name} to remove!");

        int j = 0;

        TrimmableStack? tmpEventComponentStorage = null;
        int tmpEventComponentIndex = -1;

        var destinationComponents = destination.Components;
        for (int i = 0; i < from.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                if (entityLocation.HasEvent(EntityFlags.GenericRemoveComp))
                {
                    from.Components[i].PushComponentToStack(entityLocation.ChunkIndex, entityLocation.ComponentIndex, out tmpEventComponentIndex);
                }
                continue;
            }
            destinationComponents[j++].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        ref var finalTableLocation = ref EntityTable[(uint)entity.EntityID];
        finalTableLocation.Location = nextLocation;

        _componentRemoved.Invoke(entity, component);
        ref var eventData = ref TryGetEventData(entityLocation, entity.EntityIDOnly, EntityFlags.RemoveComp | EntityFlags.GenericRemoveComp, out bool eventExist);
        if (eventExist)
        {
            eventData.Remove.NormalEvent.Invoke(entity, component);
            tmpEventComponentStorage?.InvokeEventWith(eventData.Remove.GenericEvent, entity, tmpEventComponentIndex);
        }
    }

    //Delete
    internal void DeleteEntity(Entity entity, EntityLocation entityLocation)
    {
        EntityFlags check = entityLocation.Flags | _worldEventFlags;
        if (check & EntityFlags.AllEvents != 0)
        {
            _entityDeleted.Invoke(entity);
            if (entityLocation.HasEvent(EntityFlags.OnDelete))
            {
                InvokeEvents(this, entity);
            }
            EventLookup.Remove(entity.EntityIDOnly);
        }

        DeleteEntityWithoutEvents(entity, entityLocation);

        //let the jit decide whether or not to inline
        static void InvokeEvents(World world, Entity entity)
        {
            foreach (var @event in world.EventLookup[entity.EntityIDOnly].Delete.AsSpan())
            {
                @event.Invoke(entity);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DeleteEntityWithoutEvents(Entity entity, EntityLocation entityLocation)
    {
        //entity is guaranteed to be alive here
        Entity replacedEntity = entityLocation.Archetype(this).DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        EntityTable.GetValueNoCheck(replacedEntity.EntityID) = new(entityLocation, replacedEntity.EntityVersion);
        EntityTable.GetValueNoCheck(entity.EntityID).Version = ushort.MaxValue;

        int nextVersion = entity.EntityVersion + 1;
        if (nextVersion != ushort.MaxValue)
        {
            //can't use max value as an ID, as it is used as a default value
            _recycledEntityIds.Push(new EntityIDOnly(entity.EntityID, (ushort)(nextVersion)));
        }
    }

    //Tag
    internal bool Tag(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        if (GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID))
            return false;

        Archetype from = entityLocation.Archetype(this);

        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Tag(tagID, entityLocation.ArchetypeID, ArchetypeEdgeType.AddTag),
            out bool exist);

        Archetype destination;
        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Concat(from.ArchetypeTagArray, tagID, out var res), this, from.ArchetypeTypeArray, res);
            edge = destination.ID;
        }
        else
        {
            destination = WorldArchetypeTable[edge.ID];
        }

        destination.CreateEntityLocation(entityLocation.Flags, out var nextLocation) = entity;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;

        ref var eventData = ref TryGetEventData(entityLocation, entity.EntityIDOnly, EntityFlags.Tagged, out bool eventExist);
        if (eventExist)
            eventData.Tag.Invoke(entity, tagID);

        _tagged.Invoke(entity, tagID);

        return true;
    }

    //Detach
    internal bool Detach(Entity entity, EntityLocation entityLocation, TagID tagID)
    {
        if (!GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tagID))
            return false;

        Archetype from = entityLocation.Archetype(this);
        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges,
            ArchetypeEdgeKey.Tag(tagID, from.ID, ArchetypeEdgeType.RemoveTag),
            out bool exist);

        Archetype destination;
        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Remove(from.ArchetypeTagArray, tagID, out var arr), this, from.ArchetypeTypeArray, arr);
            edge = destination.ID;
        }
        else
        {
            destination = WorldArchetypeTable[edge.ID];
        }

        destination.CreateEntityLocation(entityLocation.Flags, out var nextLocation) = entity;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;


        ref var eventData = ref TryGetEventData(entityLocation, entity.EntityIDOnly, EntityFlags.Detach, out bool eventExist);
        if (eventExist)
            eventData.Detach.Invoke(entity, tagID);

        _detached.Invoke(entity, tagID);

        return true;
    }

    private static ReadOnlySpan<T> Concat<T>(ImmutableArray<T> types, T type, out ImmutableArray<T> result)
        where T : ITypeID
    {
        if (types.IndexOf(type) != -1)
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {type.Type.Name}");

        var builder = ImmutableArray.CreateBuilder<T>(types.Length + 1);
        builder.AddRange(types);
        builder.Add(type);

        result = builder.MoveToImmutable();
        return result.AsSpan();
    }

    private static ReadOnlySpan<T> Remove<T>(ImmutableArray<T> types, T type, out ImmutableArray<T> result)
        where T : ITypeID
    {
        int index = types.IndexOf(type);
        if (index == -1)
            FrentExceptions.Throw_ComponentNotFoundException(type.Type);
        result = types.RemoveAt(index);
        return result.AsSpan();
    }
}