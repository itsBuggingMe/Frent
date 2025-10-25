using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with itself as an argument
/// </summary>
public interface IEntityUpdate : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    void Update(Entity self);
}

/// <summary>
/// Indicates a component should be updated with itself as an argument and the specified components
/// </summary>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUpdate<TArg> : IComponentBase
{
    /// <inheritdoc cref="IUpdate.Update"/>
    void Update(Entity self, ref TArg arg);
}
