using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Systems;

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IAction<TArg>
{
    void Run(ref TArg arg);
}


[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityAction<TArg>
{
    void Run(Entity entity, ref TArg arg);
}

public interface IEntityAction
{
    void Run(Entity entity);
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUniformAction<TUniform, TArg>
{
    void Run(Entity entity, TUniform uniform, ref TArg arg);
}

public interface IEntityUniformAction<TUniform>
{
    void Run(Entity entity, TUniform uniform);

}
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUniformAction<TUniform, TArg>
{
    void Run(TUniform uniform, ref TArg arg);
}//uniform only doesnt really make any sense