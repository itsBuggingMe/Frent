using Frent.Core;
using Frent.Core.Events;
using Frent.Core.Structures;
using Frent.Updating;
using Frent.Updating.Runners;
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
        if((uint)newCompCount > 16)
            FrentExceptions.Throw_InvalidOperationException("Too many components");
        
        Span<ComponentID> allComps = stackalloc ComponentID[newCompCount];
        existingComponentIDs.CopyTo(allComps);
        int j = 0;
        for(int i = existingComponentIDs.Length; i < comps.Length; i++)
            allComps[i] = comps[j++].Component;
        var tags =  currentArchetype.ArchetypeTagArray;

        var destination = Archetype.CreateOrGetExistingArchetype(allComps, tags.AsSpan(), this, null, tags);
        destination.CreateEntityLocation(out var nextELoc) = entity;

        for(int i = 0; i < currentArchetype.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(currentArchetype.Components[i], nextELoc, location);
        }

        j = 0;
        for(int i = existingComponentIDs.Length; i < currentArchetype.Components.Length; i++)
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

        destination.CreateEntityLocation(out nextLocation) = entity;

        for (int i = 0; i < from.Components.Length; i++)
        {
            destination.Components[i].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        runner = destination.Components[^1];

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable.IndexWithInt(movedDown.EntityID).Location = entityLocation;
        EntityTable.IndexWithInt(entity.EntityID).Location = nextLocation;

        ComponentAdded?.Invoke(entity);

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

        destination.CreateEntityLocation(out EntityLocation nextLocation) = entity;

        int skipIndex = GlobalWorldTables.ComponentIndex(entityLocation.ArchetypeID, component);

        if (skipIndex >= MemoryHelpers.MaxComponentCount)
            FrentExceptions.Throw_ComponentNotFoundException($"This entity doesn't have a component of type {component.Type.Name} to remove!");

        int j = 0;

        var destinationComponents = destination.Components;
        for (int i = 0; i < from.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                continue;
            }
            destinationComponents[j++].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;

        ComponentRemoved?.Invoke(entity);

        int potCompIndex = GlobalWorldTables.ComponentIndex(destination.ID, Component<OnComponentRemoved>.ID);
        if ((uint)potCompIndex < (uint)destinationComponents.Length)
        {
            var @event = ((ComponentStorage<OnComponentRemoved>)destinationComponents[potCompIndex]).Chunks[nextLocation.ChunkIndex][nextLocation.ComponentIndex];
            @event.Invoke(entity, component);
            if (@event.GenericComponentRemoved is not null)
                from.Components[skipIndex].InvokeGenericActionWith(@event.GenericComponentRemoved, entity, entityLocation.ChunkIndex, entityLocation.ChunkIndex);
        }
    }

    //Delete
    internal void DeleteEntity(Entity entity, EntityLocation entityLocation)
    {
        EntityDeleted?.Invoke(entity);
        //entity is guaranteed to be alive here
        Entity replacedEntity = entityLocation.Archetype(this).DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        EntityTable.GetValueNoCheck(replacedEntity.EntityID) = new(entityLocation, replacedEntity.EntityVersion);
        EntityTable.GetValueNoCheck(entity.EntityID) = new(EntityLocation.Default, ushort.MaxValue);
        _recycledEntityIds.Push(entity.EntityIDOnly);
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

        destination.CreateEntityLocation(out var nextLocation) = entity;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;

        int potIndex = GlobalWorldTables.ComponentIndex(from.ID, Component<OnTagged>.ID);
        if(potIndex < toRunners.Length)
        {
            ((ComponentStorage<OnTagged>)toRunners[potIndex]).Chunks[nextLocation.ChunkIndex][nextLocation.ComponentIndex]
                .Invoke(entity, tagID);
        }

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

        destination.CreateEntityLocation(out var nextLocation) = entity;

        Debug.Assert(from.Components.Length == destination.Components.Length);
        Span<IComponentRunner> fromRunners = from.Components.AsSpan();
        Span<IComponentRunner> toRunners = destination.Components.AsSpan()[..fromRunners.Length];//avoid bounds checks

        for (int i = 0; i < fromRunners.Length; i++)
            toRunners[i].PullComponentFrom(fromRunners[i], nextLocation, entityLocation);

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;

        int potIndex = GlobalWorldTables.ComponentIndex(from.ID, Component<OnDetached>.ID);
        if (potIndex < toRunners.Length)
        {
            ((ComponentStorage<OnDetached>)toRunners[potIndex]).Chunks[nextLocation.ChunkIndex][nextLocation.ComponentIndex]
                .Invoke(entity, tagID);
        }

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