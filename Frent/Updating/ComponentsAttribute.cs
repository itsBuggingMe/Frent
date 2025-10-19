namespace Frent.Updating;

/// <summary>
/// Specifies what components are required for the update method to run.
/// </summary>
/// <param name="types">The component types that must be present when updating.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IncludesComponentsAttribute(params Type[] types) : Attribute
{
    /// <summary>
    /// The component types that are required when updating.
    /// </summary>
    public Type[] Includes { get; } = types;
}

/// <summary>
/// Specifies what components would prevent the update method from running.
/// </summary>
/// <param name="types">The component types that are excluded when updating.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExcludesComponentsAttribute(params Type[] types) : Attribute
{
    /// The component types that are excluded when updating.
    public Type[] Excludes { get; } = types;
}