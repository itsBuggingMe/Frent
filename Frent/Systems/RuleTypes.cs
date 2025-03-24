namespace Frent.Systems;

/// <summary>
/// Types of rules.
/// </summary>
[Obsolete("This is unused.")]
public enum RuleTypes
{
    /// <summary>
    /// Indicates an entity must have a component or tag.
    /// </summary>
    Have,
    /// <summary>
    /// Indicates an entity must not have a component or tag.
    /// </summary>
    DoesNotHave,
}