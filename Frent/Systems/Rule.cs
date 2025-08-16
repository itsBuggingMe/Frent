using Frent.Core;

namespace Frent.Systems;

/// <summary>
/// Encapsulates a check for an entity, used to filter queries
/// </summary>
public struct Rule : IEquatable<Rule>
{
    internal RuleState RuleStateValue;
    internal ComponentID CompID;
    internal TagID TagID;

    internal bool RuleApplies(ArchetypeID archetypeID) => RuleStateValue switch
    {
        RuleState.NotComponent => !archetypeID.HasComponent(CompID),
        RuleState.HasComponent => archetypeID.HasComponent(CompID),
        RuleState.NotTag => !archetypeID.HasTag(TagID),
        RuleState.HasTag => archetypeID.HasTag(TagID),
        RuleState.IncludeDisabled => true,
        _ => throw new InvalidDataException("Rule not initialized correctly. Use one of the factory methods."),
    };

    internal bool IsSparseRule => CompID != default && CompID.IsSparseComponent;
    internal int SparseIndex => CompID.SparseIndex;

    /// <summary>
    /// Creates a rule that applies when an archetype has the specified component.
    /// </summary>
    /// <param name="compID">The ID of the component to check for.</param>
    /// <returns>A <see cref="Rule"/> that checks for the presence of a component.</returns>
    public static Rule HasComponent(ComponentID compID) => new()
    {
        RuleStateValue = RuleState.HasComponent,
        CompID = compID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype does not have the specified component.
    /// </summary>
    /// <param name="compID">The ID of the component to check for absence.</param>
    /// <returns>A <see cref="Rule"/> that checks for the absence of a component.</returns>
    public static Rule NotComponent(ComponentID compID) => new()
    {
        RuleStateValue = RuleState.NotComponent,
        CompID = compID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype has the specified tag.
    /// </summary>
    /// <param name="tagID">The ID of the tag to check for.</param>
    /// <returns>A <see cref="Rule"/> that checks for the presence of a tag.</returns>
    public static Rule HasTag(TagID tagID) => new()
    {
        RuleStateValue = RuleState.HasTag,
        TagID = tagID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype does not have the specified tag.
    /// </summary>
    /// <param name="tagID">The ID of the tag to check for absence.</param>
    /// <returns>A <see cref="Rule"/> that checks for the absence of a tag.</returns>
    public static Rule NotTag(TagID tagID) => new()
    {
        RuleStateValue = RuleState.NotTag,
        TagID = tagID
    };

    /// <summary>
    /// Determines whether this <see cref="Rule"/> is equal to another <see cref="Rule"/>.
    /// </summary>
    /// <param name="other">The <see cref="Rule"/> to compare against.</param>
    /// <returns><see langword="true"/> if the rules are equal, <see langword="false"/> otherwise.</returns>
    public bool Equals(Rule other) =>
        RuleStateValue == other.RuleStateValue &&
        CompID.Equals(other.CompID) &&
        TagID.Equals(other.TagID);

    /// <summary>
    /// Determines whether this <see cref="Rule"/> is equal to an object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true"/> if the object is a <see cref="Rule"/> and they are equal, <see langword="false"/> otherwise.</returns>
    public override bool Equals(object? obj) => obj is Rule other && Equals(other);

    /// <summary>
    /// Gets a hash code for this <see cref="Rule"/>.
    /// </summary>
    /// <returns>A hash code representing this <see cref="Rule"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(RuleStateValue, CompID, TagID);

    /// <summary>
    /// Determines whether two <see cref="Rule"/> instances are equal.
    /// </summary>
    /// <param name="left">The first rule to compare.</param>
    /// <param name="right">The second rule to compare.</param>
    /// <returns><see langword="true"/> if the rules are equal, <see langword="false"/> otherwise.</returns>
    public static bool operator ==(Rule left, Rule right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="Rule"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first rule to compare.</param>
    /// <param name="right">The second rule to compare.</param>
    /// <returns><see langword="true"/> if the rules are not equal, <see langword="false"/> otherwise.</returns>
    public static bool operator !=(Rule left, Rule right) => !left.Equals(right);

    internal enum RuleState : int
    {
        None = 0,
        HasComponent,
        NotComponent,
        HasTag,
        NotTag,
        IncludeDisabled,
    }

    /// <summary>
    /// Using this rule will include disabled entities in a query.
    /// </summary>
    public static readonly Rule IncludeDisabledRule = new Rule() { RuleStateValue = RuleState.IncludeDisabled };
}
