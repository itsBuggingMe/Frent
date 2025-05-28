using Frent.Collections;
using Frent.Updating.Runners;

namespace Frent.Updating;

/// <summary>
/// Defines an object for creating component runners
/// </summary>
/// <remarks>Used only in source generation</remarks>
internal interface IComponentBufferManager
{
    /// <summary>
    /// Used only in source generation
    /// </summary>
    internal Array Create(int capacity);
    /// <summary>
    /// Used only in source generation
    /// </summary>
    internal IDTable CreateTable();
}

internal class ComponentUpdateFactory<T> : IComponentBufferManager
{
    public Array Create(int capacity) => new T[capacity];

    public IDTable CreateTable() => new IDTable<T>();
}