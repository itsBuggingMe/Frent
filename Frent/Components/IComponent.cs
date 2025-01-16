using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IComponent : IComponentBase
{
    void Update();
}

[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IComponent<TArg> : IComponentBase
{
    void Update(ref TArg arg);
}