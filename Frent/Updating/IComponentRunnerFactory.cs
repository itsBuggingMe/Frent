namespace Frent.Updating;

/// <summary>
/// Defines an object for creating component runners
/// </summary>
/// <remarks>Used only in source generation</remarks>
public interface IComponentRunnerFactory
{
    /// <summary>
    /// Used only in source generation
    /// </summary>
    public object Create();
}

internal interface IComponentRunnerFactory<T>
{
    internal IComponentRunner<T> CreateStronglyTyped();
}