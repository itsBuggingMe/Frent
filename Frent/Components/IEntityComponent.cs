using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Components;

/// <summary>
/// Indicates a component should be updated with itself as an argument
/// </summary>
public interface IEntityComponent : IComponentBase
{
    void Update(Entity self);
}

/// <summary>
/// Indicates a component should be updated with itself as an argument and components
/// </summary>
[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityComponent<TArg> : IComponentBase
{
    void Update(Entity self, ref TArg arg);
}
