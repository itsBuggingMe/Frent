using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IEntityComponent : IComponentBase
{
    void Update(Entity self);
}

[Variadic(TArgFrom, TArgPattern, 15)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityComponent<TArg> : IComponentBase
{
    void Update(Entity self, ref TArg arg);
}
