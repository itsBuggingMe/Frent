using Frent.Core;

namespace Frent.Systems;

/// <summary>
/// Encapsulates a check for an entity, used to filter queries
/// </summary>
public struct Rule : IEquatable<Rule>
{
    private RuleState _ruleState;
    private Func<ArchetypeID, bool>? _custom;
    private ComponentID _compID;
    private TagID _tagID;

    internal bool RuleApplies(ArchetypeID archetypeID) => _ruleState switch
    {
        RuleState.NotComponent => !archetypeID.HasComponent(_compID),
        RuleState.HasComponent => archetypeID.HasComponent(_compID),
        RuleState.NotTag => !archetypeID.HasTag(_tagID),
        RuleState.HasTag => archetypeID.HasTag(_tagID),
        RuleState.CustomDelegate => _custom!(archetypeID),
        RuleState.IncludeDisabled => true,
        _ => throw new InvalidDataException("Rule not initialized correctly. Use one of the factory methods."),
    };

    /// <summary>
    /// Creates a custom delegate-based rule.
    /// </summary>
    /// <param name="rule">The custom delegate to determine rule applicability.</param>
    /// <returns>A <see cref="Rule"/> configured with the custom delegate.</returns>
    public static Rule Delegate(Func<ArchetypeID, bool> rule) => new()
    {
        _ruleState = RuleState.CustomDelegate,
        _custom = rule
    };

    /// <summary>
    /// Creates a rule that applies when an archetype has the specified component.
    /// </summary>
    /// <param name="compID">The ID of the component to check for.</param>
    /// <returns>A <see cref="Rule"/> that checks for the presence of a component.</returns>
    public static Rule HasComponent(ComponentID compID) => new()
    {
        _ruleState = RuleState.HasComponent,
        _compID = compID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype does not have the specified component.
    /// </summary>
    /// <param name="compID">The ID of the component to check for absence.</param>
    /// <returns>A <see cref="Rule"/> that checks for the absence of a component.</returns>
    public static Rule NotComponent(ComponentID compID) => new()
    {
        _ruleState = RuleState.NotComponent,
        _compID = compID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype has the specified tag.
    /// </summary>
    /// <param name="tagID">The ID of the tag to check for.</param>
    /// <returns>A <see cref="Rule"/> that checks for the presence of a tag.</returns>
    public static Rule HasTag(TagID tagID) => new()
    {
        _ruleState = RuleState.HasTag,
        _tagID = tagID
    };

    /// <summary>
    /// Creates a rule that applies when an archetype does not have the specified tag.
    /// </summary>
    /// <param name="tagID">The ID of the tag to check for absence.</param>
    /// <returns>A <see cref="Rule"/> that checks for the absence of a tag.</returns>
    public static Rule NotTag(TagID tagID) => new()
    {
        _ruleState = RuleState.NotTag,
        _tagID = tagID
    };

    /// <summary>
    /// Determines whether this <see cref="Rule"/> is equal to another <see cref="Rule"/>.
    /// </summary>
    /// <param name="other">The <see cref="Rule"/> to compare against.</param>
    /// <returns><see langword="true"/> if the rules are equal, <see langword="false"/> otherwise.</returns>
    public bool Equals(Rule other) =>
        _ruleState == other._ruleState &&
        _custom == other._custom &&
        _compID.Equals(other._compID) &&
        _tagID.Equals(other._tagID);

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
    public override int GetHashCode() => HashCode.Combine(_ruleState, _custom, _compID, _tagID);

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

    private enum RuleState : int
    {
        None = 0,
        CustomDelegate,
        HasComponent,
        NotComponent,
        HasTag,
        NotTag,
        IncludeDisabled,
    }

    public static readonly Rule IncludeDisabledRule = new Rule() { _ruleState = RuleState.IncludeDisabled };
}
