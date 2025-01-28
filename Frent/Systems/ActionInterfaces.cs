using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Systems;

/// <summary>
/// An arbitary function with one parameter
/// </summary>
/// <remarks>Used to inline query functions</remarks>
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IAction<TArg>
{
    /// Executes the function
    void Run(ref TArg arg);
}


/// <summary>
/// An arbitary function which takes the entity and one argument
/// </summary>
/// <remarks>Used to inline query functions</remarks>
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityAction<TArg>
{
    /// Executes the function
    void Run(Entity entity, ref TArg arg);
}

/// <summary>
/// An arbitary function which takes the entity
/// </summary>
/// <remarks>Used to inline query functions</remarks>
public interface IEntityAction
{
    /// Executes the function
    void Run(Entity entity);
}

/// <summary>
/// An arbitary function which takes the entity, a uniform, and one argument
/// </summary>
/// <remarks>Used to inline query functions</remarks>
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUniformAction<TUniform, TArg>
{
    /// Executes the function
    void Run(Entity entity, TUniform uniform, ref TArg arg);
}

/// <summary>
/// An arbitary function which takes the entity and a uniform
/// </summary>
/// <remarks>Used to inline query functions</remarks>
public interface IEntityUniformAction<TUniform>
{
    /// Executes the function
    void Run(Entity entity, TUniform uniform);

}

/// <summary>
/// An arbitary function which takes a uniform and one argument
/// </summary>
/// <remarks>Used to inline query functions</remarks>
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUniformAction<TUniform, TArg>
{
    /// Executes the function
    void Run(TUniform uniform, ref TArg arg);
}//uniform only doesnt really make any sense