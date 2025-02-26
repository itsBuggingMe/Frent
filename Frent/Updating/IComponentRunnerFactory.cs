using Frent.Updating.Runners;

namespace Frent.Updating;

/// <summary>
/// Defines an object for creating component runners
/// </summary>
/// <remarks>Used only in source generation</remarks>
public interface IComponentStorageBaseFactory
{
    /// <summary>
    /// Used only in source generation
    /// </summary>
    public object Create();
    /// <summary>
    /// Used only in source generation
    /// </summary>
    public object CreateStack();
}

internal interface IComponentStorageBaseFactory<T>
{
    internal ComponentStorage<T> CreateStronglyTyped();
}