using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with itself as an argument
/// </summary>
/// <remarks>Components should only implement one "Update" method.</remarks>
public interface IEntityComponent : IComponentBase
{
    /// <inheritdoc cref="IComponent.Update"/>
    void Update(Entity self);
}

/// <summary>
/// Indicates a component should be updated with itself as an argument and the specified components
/// </summary>
/// <remarks>Components should only implement one "Update" method.</remarks>
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityComponent<TArg> : IComponentBase
{
    /// <inheritdoc cref="IComponent.Update"/>
    void Update(Entity self, ref TArg arg);
}
