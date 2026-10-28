using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Systems;

/// <summary>
/// An arbitrary function with one parameter
/// </summary>
/// <remarks>Used in inline query functions</remarks>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IAction<TArg>
{
    /// Executes the function
    void Run(ref TArg arg);
}

/// <summary>
/// An arbitrary function with an entity as a parameter and a component reference.
/// </summary>
/// <remarks>Used in inline query functions</remarks>
/// <variadic />
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityAction<TArg>
{
    /// Executes the function
    void Run(Entity e, ref TArg arg);
}