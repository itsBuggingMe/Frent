using static Frent.Components.Variadics;
using Frent.Variadic.Generator;

namespace Frent.Components;

public interface IUpdateComponent : IComponent
{
    void Update();
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUpdateComponent<TArg> : IComponent
{
    void Update(ref TArg arg);
}