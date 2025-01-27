using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IEntityUniformComponent<TUniform> : IComponentBase
{
    public void Update(Entity eselfntity, TUniform uniform);
}

[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUniformComponent<TUniform, TArg> : IComponentBase
{
    public void Update(Entity self, TUniform uniform, ref TArg arg);
}