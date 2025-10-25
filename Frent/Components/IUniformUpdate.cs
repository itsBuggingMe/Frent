using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with a uniform of type <typeparamref name="TUniform"/>
/// </summary>
public interface IUniformUpdate<TUniform> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    void Update(TUniform uniform);
}

/// <summary>
/// Indicates a component should be updated with a uniform of type <typeparamref name="TUniform"/> and the specified components
/// </summary>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUniformUpdate<TUniform, TArg> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    void Update(TUniform uniform, ref TArg arg);
}