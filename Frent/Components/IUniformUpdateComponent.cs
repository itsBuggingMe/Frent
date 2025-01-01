using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

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