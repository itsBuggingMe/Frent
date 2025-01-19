using System.Collections.Immutable;

namespace Frent.Systems;


/// <summary>
/// API Consumers should not implement this interface. Use existing implementations.
/// </summary>
public interface IConstantQueryHashProvider
{
    /// <summary>
    /// API Consumers should not implement this interface
    /// </summary>
    public ImmutableArray<Rule> Rules { get; }
    /// <summary>
    /// API Consumers should not implement this interface
    /// </summary>
    public int ToHashCode();
}
