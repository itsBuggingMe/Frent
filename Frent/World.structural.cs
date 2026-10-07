using Frent.Collections;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Core.Events;
using Frent.Systems;
using Frent.Updating;
using System.Collections.Immutable;
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

        Archetype.MoveLinks(this, from, destination, currentLookup.Index, deletedIndex, nextLocation.Index);

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
        // displacedEntityLocation.Archetype = currentLookup.Archetype;
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

        Archetype.MoveLinks(this, from, destination, currentLookup.Index, deletedIndex, nextLocation.Index);

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
        // displacedEntityLocation.Archetype = currentLookup.Archetype;
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

        Archetype.MoveLinks(this, from, destination, currentLookup.Index, deletedIndex, nextLocation.Index);

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
        // displacedEntityLocation.Archetype = currentLookup.Archetype;
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
        if (currentLookup.HasFlag(EntityFlags.HasHadSparseComponents))
            CleanupSparseComponents(entity, ref currentLookup);

        // entity is guaranteed to be alive here
        // entity is alive; Archetype is not null
        if (currentLookup.HasFlag(EntityFlags.HasHadLinks))
        {
            int worldLinkId = currentLookup.Archetype.GetExistingLinkID(currentLookup.Index);
            UnlinkAll(worldLinkId);
            RecycleLinkID(worldLinkId);
        }
        EntityIDOnly replacedEntity = currentLookup.Archetype!.DeleteEntity(this, currentLookup.Index);

        Debug.Assert(replacedEntity.ID < EntityTable._buffer.Length);
        Debug.Assert(entity.EntityID < EntityTable._buffer.Length);

        ref var replaced = ref EntityTable.UnsafeIndexNoResize(replacedEntity.ID);
        replaced.Index = currentLookup.Index;
        // replaced.Archetype = currentLookup.Archetype;

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

    internal void RemoveEntityPlaceholder(ref EntityLocation currentLookup)
    {
        if (currentLookup.HasFlag(EntityFlags.HasHadLinks))
        {
            int worldLinkId = currentLookup.Archetype.GetExistingLinkID(currentLookup.Index);
            UnlinkAll(worldLinkId);
            RecycleLinkID(worldLinkId);
        }
        EntityIDOnly replacedEntity = currentLookup.Archetype!.DeleteEntity(this, currentLookup.Index);

        ref var replaced = ref EntityTable.UnsafeIndexNoResize(replacedEntity.ID);
        replaced.Index = currentLookup.Index;
        // replaced.Archetype = currentLookup.Archetype;
    }

    /// <summary>
    /// Adds component <typeparamref name="T"/> to every entity that matches <paramref name="query"/>.
    /// </summary>
    /// <remarks>
    /// Entities that already have the component are skipped. All matching entities are moved
    /// at once per archetype instead of one at a time.
    /// </remarks>
    public void AddComponent<T>(Query query) => AddComponentCore<T>(query, default!, false);

    /// <inheritdoc cref="AddComponent{T}(Query)"/>
    /// <param name="component">The component value to add to every entity.</param>
    public void AddComponent<T>(Query query, in T component) => AddComponentCore(query, component, true);

    /// <summary>
    /// Removes component <typeparamref name="T"/> from every entity that matches <paramref name="query"/>.
    /// </summary>
    /// <inheritdoc cref="AddComponent{T}(Query)" path="/remarks"/>
    public void RemoveComponent<T>(Query query)
    {
        if (query.World != this)
            FrentExceptions.Throw_ArgumentException("The query was created on a different world.");

        ComponentID compId = Component<T>.ID;
        int sparseIndex = compId.SparseIndex;

        if (!AllowStructualChanges)
        {
            if (sparseIndex != 0)
            {
                ComponentSparseSetBase sparseSet = WorldSparseSetTable.UnsafeArrayIndex(sparseIndex);
                foreach (var arch in query.AsSpan())
                {
                    Span<EntityIDOnly> entities = arch.GetEntitySpan();
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (sparseSet.Has(entities[i].ID))
                            WorldUpdateCommandBuffer.RemoveComponent(entities[i].ToEntity(this), compId);
                    }
                }
            }
            else
            {
                foreach (var arch in query.AsSpan())
                {
                    if ((arch.ComponentTagTable.UnsafeArrayIndex(compId.RawIndex) & GlobalWorldTables.IndexBits) == 0)
                        continue;

                    Span<EntityIDOnly> entities = arch.GetEntitySpan();
                    for (int i = 0; i < entities.Length; i++)
                        WorldUpdateCommandBuffer.RemoveComponent(entities[i].ToEntity(this), compId);
                }
            }
            return;
        }

        if (sparseIndex != 0)
        {
            ComponentSparseSetBase sparseSet = WorldSparseSetTable.UnsafeArrayIndex(sparseIndex);
            foreach (var arch in query.AsSpan())
            {
                Span<EntityIDOnly> entities = arch.GetEntitySpan();
                for (int i = 0; i < entities.Length; i++)
                {
                    if (sparseSet.Has(entities[i].ID))
                        entities[i].ToEntity(this).Remove(compId);
                }
            }
            return;
        }

        foreach (var from in query.AsSpan())
        {
            if ((from.ComponentTagTable.UnsafeArrayIndex(compId.RawIndex) & GlobalWorldTables.IndexBits) == 0)
                continue;

            int count = from.EntityCount;
            ComponentStorageRecord storage = from.GetComponentStorage(compId);
            EntityIDOnly[] ids = from.EntityIDArray;
            for (int r = 0; r < count; r++)
            {
                EntityIDOnly eid = ids.UnsafeArrayIndex(r);
                Entity e = eid.ToEntity(this);

                ref EntityLocation loc = ref EntityTable.UnsafeIndexNoResize(eid.ID);
                EntityFlags flags = loc.Flags;
                if (!EntityLocation.HasEventFlag(flags | WorldEventFlags, EntityFlags.RemoveComp | EntityFlags.RemoveGenericComp))
                    continue;

                ComponentRemovedEvent.Invoke(e, compId);

                if (!EntityLocation.HasEventFlag(flags, EntityFlags.RemoveComp | EntityFlags.RemoveGenericComp))
                    continue;

                ref EventRecord events = ref EventLookup.GetValueRefOrNullRef(eid);
                events.Remove.NormalEvent.Invoke(e, compId);
                if (events.Remove.GenericEvent is { } generic)
                    storage.InvokeGenericActionWith(generic, e, r);
            }

            Archetype dest = RemoveComponentLookup.FindAdjacentArchetypeID(compId, from.ID, this, ArchetypeEdgeType.RemoveComponent).Archetype(this);
            from.DrainEntitiesInto(this, dest);
        }
    }

    private void AddComponentCore<T>(Query query, in T component, bool hasValue)
    {
        if (query.World != this)
            FrentExceptions.Throw_ArgumentException("The query was created on a different world.");

        ComponentID compId = Component<T>.ID;
        int sparseIndex = compId.SparseIndex;

        if (!AllowStructualChanges)
        {
            if (sparseIndex != 0)
            {
                ComponentSparseSetBase sparseSet = WorldSparseSetTable.UnsafeArrayIndex(sparseIndex);
                foreach (var arch in query.AsSpan())
                {
                    Span<EntityIDOnly> entities = arch.GetEntitySpan();
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (!sparseSet.Has(entities[i].ID))
                            WorldUpdateCommandBuffer.AddComponent(entities[i].ToEntity(this), component);
                    }
                }
            }
            else
            {
                foreach (var arch in query.AsSpan())
                {
                    if ((arch.ComponentTagTable.UnsafeArrayIndex(compId.RawIndex) & GlobalWorldTables.IndexBits) != 0)
                        continue;

                    Span<EntityIDOnly> entities = arch.GetEntitySpan();
                    for (int i = 0; i < entities.Length; i++)
                        WorldUpdateCommandBuffer.AddComponent(entities[i].ToEntity(this), component);
                }
            }
            return;
        }

        if (sparseIndex != 0)
        {
            ComponentSparseSetBase sparseSet = WorldSparseSetTable.UnsafeArrayIndex(sparseIndex);
            foreach (var arch in query.AsSpan())
            {
                Span<EntityIDOnly> entities = arch.GetEntitySpan();
                for (int i = 0; i < entities.Length; i++)
                {
                    if (!sparseSet.Has(entities[i].ID))
                        entities[i].ToEntity(this).Add(component);
                }
            }
            return;
        }

        foreach (var from in query.AsSpan())
        {
            if ((from.ComponentTagTable.UnsafeArrayIndex(compId.RawIndex) & GlobalWorldTables.IndexBits) != 0)
                continue;

            Archetype dest = AddComponentLookup.FindAdjacentArchetypeID(compId, from.ID, this, ArchetypeEdgeType.AddComponent).Archetype(this);
            int count = from.EntityCount;
            int start = dest.EntityCount;
            from.DrainEntitiesInto(this, dest);

            ComponentStorageRecord col = dest.Components.UnsafeArrayIndex(dest.GetComponentIndex(compId));
            if (hasValue)
                UnsafeExtensions.UnsafeCast<T[]>(col.Buffer).AsSpan(start, count).Fill(component);

            bool hasIniter = Component<T>.Initer is not null;
            Span<EntityIDOnly> moved = dest.EntityIDArray.AsSpan(start, count);
            for (int i = 0; i < moved.Length; i++)
            {
                ref EntityLocation loc = ref EntityTable.UnsafeIndexNoResize(moved[i].ID);
                EntityFlags flags = loc.Flags;
                if (!hasIniter && !EntityLocation.HasEventFlag(flags | WorldEventFlags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
                    continue;

                Entity e = moved[i].ToEntity(this);
                int index = start + i;

                if (hasIniter)
                    col.CallIniter(e, index);

                ComponentAddedEvent.Invoke(e, compId);

                if (!EntityLocation.HasEventFlag(flags, EntityFlags.AddComp | EntityFlags.AddGenericComp))
                    continue;

                ref EventRecord events = ref EventLookup.GetValueRefOrNullRef(moved[i]);
                events.Add.NormalEvent.Invoke(e, compId);
                if (events.Add.GenericEvent is { } generic)
                    col.InvokeGenericActionWith(generic, e, index);
            }
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
