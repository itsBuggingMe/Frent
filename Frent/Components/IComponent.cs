﻿using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with zero arguments
/// </summary>
/// <remarks>Components should only implement one "Update" method.</remarks>
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
/// <remarks>Components should only implement one "Update" method.</remarks>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IComponent<TArg> : IComponentBase
{
    /// <inheritdoc cref="IComponent.Update"/>
    void Update(ref TArg arg);
}