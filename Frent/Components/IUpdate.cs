using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with zero arguments
/// </summary>
public interface IUpdate : IComponentBase
{
    /// <summary>
    /// Updates this component
    /// </summary>
    void Update();
}

/// <summary>
/// Indicates a component should be updated with the specified components
/// </summary>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUpdate<TArg> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    void Update(ref TArg arg);
}