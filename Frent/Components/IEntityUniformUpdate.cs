using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with itself as an argument and a uniform of type <typeparamref name="TUniform"/>
/// </summary>
public interface IEntityUniformUpdate<TUniform> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    public void Update(Entity self, TUniform uniform);
}

/// <summary>
/// Indicates a component should be updated with itself as an argument and a uniform of type <typeparamref name="TUniform"/>, along with the specified components
/// </summary>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUniformUpdate<TUniform, TArg> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    public void Update(Entity self, TUniform uniform, ref TArg arg);
}