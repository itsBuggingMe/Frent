using Frent.Collections;

namespace Frent.Systems.Queries;

/// <summary>
/// A variadic type for building queries. Should not be implemented manually.
/// </summary>
public interface IQueryBuilder
{
    /// <inheritdoc cref="IQueryBuilder"/>
    void AddRules(List<Rule> rules);
    /// <inheritdoc cref="IQueryBuilder"/>
    World World { get; }
}