using Frent.Collections;
using Frent.Core;

namespace Frent.Updating.Runners;
internal class NoneUpdate<TComp> : ComponentRunnerBase<NoneUpdate<TComp>, TComp>
{
    public override void Run(Archetype b) { }
}

public class NoneUpdateRunnerFactory<T> : IComponentRunnerFactory, IComponentRunnerFactory<T>
{
    public object Create() => new NoneUpdate<T>();
    public object CreateStack() => new TrimmableStack<T>();
    IComponentRunner<T> IComponentRunnerFactory<T>.CreateStronglyTyped() => new NoneUpdate<T>();
}