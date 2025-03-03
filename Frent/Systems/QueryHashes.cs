using Frent.Core;

namespace Frent.Systems;

/// <summary>
/// Specifies a query should have a component of <see paramref="T"/>
/// </summary>
public struct With<T> : IRuleProvider
{
    /// <summary>
    /// The rule.
    /// </summary>
    public Rule Rule => Rule.HasComponent(Component<T>.ID);
}

/// <summary>
/// Specifies a query should have a tag of <see paramref="T"/>
/// </summary>
public struct Tagged<T> : IRuleProvider
{
    /// <summary>
    /// The rule.
    /// </summary>
    public Rule Rule => Rule.HasTag(Tag<T>.ID);
}

/// <summary>
/// Specifies a query should not have a component of <see paramref="T"/>
/// </summary>
public struct Not<T> : IRuleProvider
{
    /// <summary>
    /// The rule.
    /// </summary>
    public Rule Rule => Rule.NotComponent(Component<T>.ID);
}

/// <summary>
/// Specifies a query should not have a tag of <see paramref="T"/>
/// </summary>
public struct Untagged<T> : IRuleProvider
{
    /// <summary>
    /// The rule.
    /// </summary>
    public Rule Rule => Rule.NotTag(Tag<T>.ID);
}

/// <summary>
/// Specifies a query should include all entities
/// </summary>
public struct IncludeDisabled : IRuleProvider
{
    /// <summary>
    /// The rule.
    /// </summary>
    public Rule Rule => Rule.IncludeDisabledRule;
}