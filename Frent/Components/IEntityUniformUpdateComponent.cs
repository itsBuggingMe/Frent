using static Frent.Components.Variadics;
using Frent.Variadic.Generator;

namespace Frent.Components;

public interface IEntityUniformUpdateComponent<TUniform> : IComponent
{
    public void Update(Entity entity, in TUniform uniform);
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUniformUpdateComponent<TUniform, TArg> : IComponent
{
    public void Update(Entity entity, in TUniform uniform, ref TArg arg);
}