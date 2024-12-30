using static Frent.Components.Variadics;
using Frent.Variadic.Generator;

namespace Frent.Components;

public interface IUniformUpdateComponent<TUniform> : IComponent
{
    void Update(in TUniform uniform);
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IUniformUpdateComponent<TUniform, TArg> : IComponent
{
    void Update(in TUniform uniform, ref TArg arg);
}