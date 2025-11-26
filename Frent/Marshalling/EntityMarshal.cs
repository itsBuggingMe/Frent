using Frent.Core;

namespace Frent.Marshalling;

/// <summary>
/// Unsafe methods to write even faster code! Users are expected to know what they are doing and improper usage can result in corrupting world state and segfaults.
/// </summary>
/// <remarks>The APIs in this class are less stable, as many depend on implementation details.</remarks>
public static class EntityMarshal
{
    /// <summary>
    /// Gets the <see cref="World"/> for an <see cref="Entity"/>. Does not check if the <see cref="Entity"/> or <see cref="World"/> is alive.
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to get the <see cref="World"/> for.</param>
    /// <returns>The <see cref="World"/> the <paramref name="entity"/> belongs to, if it still exists.</returns>
    public static World? GetWorld(Entity entity)
    {
        return GlobalWorldTables.Worlds.UnsafeIndexNoResize(entity.EntityID);
    }

    /// <summary>
    /// Gets the raw entityID from an <see cref="Entity"/>
    /// </summary>
    /// <returns>The integer entityID</returns>
    public static int EntityID(Entity entity)
    {
        return entity.EntityID;
    }
}
