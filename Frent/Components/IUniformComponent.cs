using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IUniformComponent<TUniform> : IComponentBase
{
    void Update(in TUniform uniform);
}

[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUniformComponent<TUniform, TArg> : IComponentBase
{
    void Update(in TUniform uniform, ref TArg arg);
}