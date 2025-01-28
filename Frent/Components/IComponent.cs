using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with zero arguments
/// </summary>
public interface IComponent : IComponentBase
{
    /// <summary>
    /// Updates this component
    /// </summary>
    void Update();
}

/// <summary>
/// Indicates a component should be updated with the specified components
/// </summary>
[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IComponent<TArg> : IComponentBase
{
    /// <summary>
    /// Updates this component
    /// </summary>
    void Update(ref TArg arg);
}