using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IUpdateComponent : IComponent
{
    void Update();
}

[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUpdateComponent<TArg> : IComponent
{
    void Update(ref TArg arg);
}