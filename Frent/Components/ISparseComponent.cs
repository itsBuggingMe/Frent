namespace Frent.Components;

/// <summary>
/// Marks a component to be stored in sparse sets instead of archetypes.
/// </summary>
/// <remarks>Pros: Faster add/remove operations; lower archetype fragmentation. Cons: Slower systems, less contiguous components, increased memory overhead.</remarks>
public interface ISparseComponent : IComponentBase;