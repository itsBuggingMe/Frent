using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent;

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IQuery<TArg>
{
    void Run(ref TArg arg);
}


[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IQueryEntity<TArg>
{
    void Run(Entity entity, ref TArg arg);
}

public interface IQueryEntity
{
    void Run(Entity entity);
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IQueryEntityUniform<TUniform, TArg>
{
    void Run(Entity entity, in TUniform uniform, ref TArg arg);
}

public interface IQueryEntityUniform<TUniform>
{
    void Run(Entity entity, in TUniform uniform);

}
[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IQueryUniform<TUniform, TArg>
{
    void Run(in TUniform uniform, ref TArg arg);
}//uniform only doesnt really make any sense