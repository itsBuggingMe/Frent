using Frent.Core;
using Frent.Updating;
using Frent.Updating.Runners;
using System.Dynamic;

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
    /// <returns>A reference to the component in memory.</returns>
    public static ref T GetComponent<T>(World world, Entity entity) => ref Get<T>(world, entity.EntityID);

    /// <summary>
    /// Gets raw span over the entire buffer of a component type for an archetype.
    /// </summary>
    /// <typeparam name="T">The type of component to get.</typeparam>
    /// <param name="world">The world that the entity belongs to.</param>
    /// <param name="entity">The entity whose component buffer to get.</param>
    /// <param name="index">The index of the entity's component.</param>
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
}
