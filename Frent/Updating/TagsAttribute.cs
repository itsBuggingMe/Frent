namespace Frent.Updating;

/// <summary>
/// Specifies what tags are required for the update method to run.
/// </summary>
/// <param name="types">The tag types that must be present when updating.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IncludesTagsAttribute(params Type[] types) : Attribute
{
    /// <summary>
    /// The tag types that are required when updating.
    /// </summary>
    public Type[] Includes { get; } = types;
}

/// <summary>
/// Specifies what tags would prevent the update method from running.
/// </summary>
/// <param name="types">The tag types that are excluded when updating.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExcludesTagsAttribute(params Type[] types) : Attribute
{
    /// <summary>
    /// The tag types that are excluded when updating.
    /// </summary>
    public Type[] Excludes { get; } = types;
}