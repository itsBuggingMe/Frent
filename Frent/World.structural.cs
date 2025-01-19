using Frent.Core;
using Frent.Updating;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent;

partial class World
{
    /*  
     *  This file contains all core functions related to structual changes on the world
     *  The only core structual change function not here is create, since it needs to be source generated
     *  These functions take all the data it needs, with no validation that an entity is alive
     */ 

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

        if(!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(Concat(from.ArchetypeTypeArray, component.Type, out var res), from.ArchetypeTagArray.AsSpan(), this, res, from.ArchetypeTagArray);
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

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;
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
            destination = Archetype.CreateOrGetExistingArchetype(Remove(from.ArchetypeTypeArray, component.Type, out var arr), from.ArchetypeTagArray.AsSpan(), this, arr, from.ArchetypeTagArray);
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

        for (int i = 0; i < from.Components.Length; i++)
        {
            if (i == skipIndex)
            {
                continue;
            }
            destination.Components[j++].PullComponentFrom(from.Components[i], nextLocation, entityLocation);
        }

        Entity movedDown = from.DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);

        EntityTable[(uint)movedDown.EntityID].Location = entityLocation;
        EntityTable[(uint)entity.EntityID].Location = nextLocation;
    }

    //Delete
    internal void DeleteEntity(int entityID, ushort version, EntityLocation entityLocation)
    {
        //entity is guaranteed to be alive here
        Entity replacedEntity = entityLocation.Archetype(this).DeleteEntity(entityLocation.ChunkIndex, entityLocation.ComponentIndex);
        EntityTable[(uint)replacedEntity.EntityID] = new(entityLocation, replacedEntity.EntityVersion);
        EntityTable[(uint)entityID] = new(EntityLocation.Default, ushort.MaxValue);
        _recycledEntityIds.Push((entityID, version));
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
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Concat(from.ArchetypeTagArray, tagID.Type, out var res), this, from.ArchetypeTypeArray, res);
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

        return true;
    }

    //Detach
    internal bool Detach(Entity entity, EntityLocation entityLocation, TagID tag)
    {
        if (!GlobalWorldTables.HasTag(entityLocation.ArchetypeID, tag))
            return false;

        Archetype from = entityLocation.Archetype(this);
        ref var edge = ref CollectionsMarshal.GetValueRefOrAddDefault(ArchetypeGraphEdges, 
            ArchetypeEdgeKey.Tag(tag, from.ID, ArchetypeEdgeType.RemoveTag), 
            out bool exist);

        ref Archetype destination = ref Unsafe.NullRef<Archetype>();
        if (!exist)
        {
            destination = Archetype.CreateOrGetExistingArchetype(from.ArchetypeTypeArray.AsSpan(), Remove(from.ArchetypeTagArray, tag.Type, out var arr), this, from.ArchetypeTypeArray, arr);
            edge = destination.ID;
        }
        else
        {
            destination = ref WorldArchetypeTable[edge.ID];
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

        return true;
    }

    private static ReadOnlySpan<Type> Concat(ImmutableArray<Type> types, Type type, out ImmutableArray<Type> result)
    {
        if (types.IndexOf(type) != -1)
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {type.Name}");

        var builder = ImmutableArray.CreateBuilder<Type>(types.Length + 1);
        builder.AddRange(types);
        builder.Add(type);

        result = builder.MoveToImmutable();
        return result.AsSpan();
    }

    private static ReadOnlySpan<Type> Remove(ImmutableArray<Type> types, Type type, out ImmutableArray<Type> result)
    {
        int index = types.IndexOf(type);
        if (index == -1)
            FrentExceptions.Throw_ComponentNotFoundException(type);
        result = types.RemoveAt(index);
        return result.AsSpan();
    }
}