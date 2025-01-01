using Frent.Variadic.Generator;
using static Frent.Components.Variadics;

namespace Frent.Components;

public interface IEntityUpdateComponent : IComponent
{
    void Update(Entity entity);
}

[Variadic(TArgFrom, TArgPattern)]
[Variadic(RefArgFrom, RefArgPattern)]
public interface IEntityUpdateComponent<TArg> : IComponent
{
    void Update(Entity entity, ref TArg arg);
}
