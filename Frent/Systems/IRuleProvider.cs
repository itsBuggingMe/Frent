namespace Frent.Systems;


/// <summary>
/// API Consumers should not implement this interface. Use existing implementations.
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// API Consumers should not manually implement this interface
    /// </summary>
    public Rule Rule { get; }
}
