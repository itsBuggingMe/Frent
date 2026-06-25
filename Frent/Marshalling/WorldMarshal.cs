using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Updating;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Frent.Marshalling;

/// <summary>
/// Unsafe methods to write even faster code! Users are expected to know what they are doing and improper usage can result in corrupting world state and segfaults.
/// </summary>
/// <remarks>The APIs in this class are less stable, as many depend on implementation details.</remarks>
public static class WorldMarshal
{
    /// <summary>
    /// Gets a component of an entity, without checking if the entity has the component or if the world belongs to the entity.
    /// </summary>
    /// <remarks>Component must be an archetypical component.</remarks>
    /// <returns>A reference to the component in memory.</returns>
    public static ref T GetComponent<T>(World world, Entity entity) => ref Get<T>(world, entity.EntityID);

    /// <summary>
    /// Gets raw span over the entire buffer of a component type for an archetype.
    /// </summary>
    /// <typeparam name="T">The type of component to get.</typeparam>
    /// <param name="world">The world that the entity belongs to.</param>
    /// <param name="entity">The entity whose component buffer to get.</param>
    /// <param name="index">The index of the entity's component.</param>
    /// <remarks>Component must be an archetypical component.</remarks>
    /// <returns>The entire unsliced raw buffer. May be larger than the number of entities in an archetype.</returns>
    public static Span<T> GetRawBuffer<T>(World world, Entity entity, out int index)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entity.EntityID);
        index = location.Index;
        return UnsafeExtensions.UnsafeCast<T[]>(location.Archetype.GetComponentStorage<T>().Buffer).AsSpan();
    }

    /// <summary>
    /// Gets a component of an entity from a raw entityID.
    /// </summary>
    /// <remarks>Component must be an archetypical component.</remarks>
    /// <returns>A reference to the component in memory.</returns>
    public static ref T Get<T>(World world, int entityID)
    {
        EntityLocation location = world.EntityTable.UnsafeIndexNoResize(entityID);

        Archetype archetype = location.Archetype;

        int compIndex = archetype.GetComponentIndex<T>();

        //Components[0] null; trap
        ComponentStorageRecord storage = archetype.Components.UnsafeArrayIndex(compIndex);
        return ref storage.UnsafeIndex<T>(location.Index);
    }

    /// <summary>
    /// Gets the raw sparse set data for a component from a world.
    /// </summary>
    /// <typeparam name="T">The type of component to get/</typeparam>
    /// <param name="world">The world to get the sparse set from.</param>
    /// <param name="components">The raw unsliced buffer of components.</param>
    /// <param name="ids">A record of which entity a component belongs to in the <paramref name="components"/> span.</param>
    /// <param name="sparse">A mapping from an entity's id to the component index in the <paramref name="components"/> span.</param>
    /// <returns>The number of entities with a component of type <typeparamref name="T"/> in the <paramref name="world"/>.</returns>
    public static int GetSparseSet<T>(World world, out Span<T> components, out Span<int> ids, out Span<int> sparse)
        where T : ISparseComponent
    {
        int index = Component<T>.SparseSetComponentIndex;
        ComponentSparseSet<T> set = UnsafeExtensions.UnsafeCast<ComponentSparseSet<T>>(world.WorldSparseSetTable[index]);
        components = set.Dense;
        ids = set.IDSpan();
        sparse = set.SparseSpan();
        return set.Count;
    }

    /// <summary>
    /// Creates an entity with a specific ID. 
    /// </summary>
    /// <remarks>Can create holes in the entity table!</remarks>
    /// <returns>An <see cref="Entity"/> instance.</returns>
    public static Entity CreateEntityWithID(World world, int entityId)
    {
        ref EntityLocation entityLoc = ref world.EntityTable[entityId];

        if (entityLoc.Archetype is not null)
            throw new InvalidOperationException("Entity with this Id exists already.");

        world.NextEntityID = Math.Max(world.NextEntityID, entityId + 1);

        ref EntityIDOnly archetypeRecord = ref world.DefaultArchetype.CreateEntityLocation(EntityFlags.None, out entityLoc);

        Entity result = new(world.WorldID, entityLoc.Version, entityId);
        archetypeRecord.Init(result);

        return result;
    }

    /// <summary>
    /// Copies an entity and its component data into another world.
    /// </summary>
    /// <param name="destinationWorld">The world that receives the copied entity.</param>
    /// <param name="entity">The entity to copy.</param>
    /// <param name="callIniters">Whether to invoke init callbacks on copied components.</param>
    /// <param name="callEvents">Whether to invoke the destination world's entity created event.</param>
    /// <returns>The copied entity in <paramref name="destinationWorld"/>.</returns>
    public static Entity MoveEntityAcrossWorlds(World destinationWorld, Entity entity, bool callIniters, bool callEvents)
    {
        ref EntityLocation eloc = ref entity.AssertIsAlive(out World sourceWorld);
        if (sourceWorld == destinationWorld)
            return entity;
        if (!destinationWorld.AllowStructualChanges)
            throw new InvalidOperationException("Cannot perform structural changes on destination world.");

        // different world, same components; same order
        Archetype sourceArchetype = eloc.Archetype;
        Archetype destinationArchetype = Archetype.CreateOrGetExistingArchetype(eloc.ArchetypeID, destinationWorld).Archetype;

        EntityFlags destinationFlags = eloc.HasFlag(EntityFlags.HasHadSparseComponents) ? EntityFlags.HasHadSparseComponents : EntityFlags.None;
        ref EntityIDOnly destinationIdSlot = ref destinationArchetype.CreateEntityLocation(destinationFlags, out var destinationLocation);
        Entity copiedEntity = destinationWorld.CreateEntityFromLocation(destinationLocation);
        destinationIdSlot.Init(copiedEntity);

        for (int i = 1; i < sourceArchetype.Components.Length; i++)
        {
            Array.Copy(sourceArchetype.Components[i].Buffer, eloc.Index, destinationArchetype.Components[i].Buffer, destinationLocation.Index, 1);
        }

        if (eloc.HasFlag(EntityFlags.HasHadSparseComponents))
        {
            ref Bitset bitset = ref sourceArchetype.GetBitset(eloc.Index);
            destinationArchetype.GetBitset(destinationLocation.Index) = bitset;
            ComponentSparseSetBase[] srcSparseSets = sourceWorld.WorldSparseSetTable;
            ComponentSparseSetBase[] destSparseSets = destinationWorld.WorldSparseSetTable;

            foreach (int sparseComponentId in bitset)
            {
                ComponentSparseSetBase srcSet = srcSparseSets.UnsafeArrayIndex(sparseComponentId);
                ComponentSparseSetBase destSet = destSparseSets.UnsafeArrayIndex(sparseComponentId);

                srcSet.CopyTo(entity.EntityID, destSet, copiedEntity.EntityID);
            }

            if (callIniters)
            {
                foreach (int sparseComponentId in bitset)
                {
                    destSparseSets.UnsafeArrayIndex(sparseComponentId).Init(copiedEntity);
                }
            }
        }

        if (callIniters)
        {
            for (int i = 1; i < destinationArchetype.Components.Length; i++)
            {
                destinationArchetype.Components[i].CallIniter(copiedEntity, destinationLocation.Index);
            }
        }

        if (callEvents)
        {
            destinationWorld.EntityCreatedEvent.Invoke(copiedEntity);
        }

        return copiedEntity;
    }
}
