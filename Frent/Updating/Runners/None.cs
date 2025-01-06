using Frent.Core;

namespace Frent.Updating.Runners;
internal class None<TComp> : ComponentRunnerBase<None<TComp>, TComp>
{
    public override void Run(Archetype b) { }
}

public class NoneComponentRunnerFactory<T> : IComponentRunnerFactory, IComponentRunnerFactory<T>
{
    public object Create() => new None<T>();
    IComponentRunner<T> IComponentRunnerFactory<T>.CreateStronglyTyped() => new None<T>();
}