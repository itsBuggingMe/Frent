using Frent.Core;

namespace Frent.Systems;

/// <summary>
/// Provides read access to the raw ids of the entities in a chunk, without constructing <see cref="Entity"/> instances.
/// </summary>
/// <remarks>
/// Ids identify an entity's storage slot within its <see cref="World"/>; compare against <see cref="Entity"/> handles when a full identity including version is required.
/// </remarks>
public ref struct EntityIDSpan
{
    private Span<EntityIDOnly> _ids;

    internal EntityIDSpan(Span<EntityIDOnly> ids)
    {
        _ids = ids;
    }

    /// <summary>
    /// Gets the id of the entity at the given index, aligned with any component spans enumerated alongside it.
    /// </summary>
    public readonly int this[int index] => _ids[index].ID;

    /// <summary>
    /// The number of entity ids in this span.
    /// </summary>
    public readonly int Length => _ids.Length;
}
