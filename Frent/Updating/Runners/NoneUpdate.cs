using Frent.Collections;
using Frent.Core;

namespace Frent.Updating.Runners;
internal class NoneUpdate<TComp> : ComponentRunnerBase<NoneUpdate<TComp>, TComp>
{
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) { }
    public override void Run(World world, Archetype b) { }
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
public class NoneUpdateRunnerFactory<T> : IComponentRunnerFactory, IComponentRunnerFactory<T>
{
    /// <inheritdoc/>
    public object Create() => new NoneUpdate<T>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<T>();
    IComponentRunner<T> IComponentRunnerFactory<T>.CreateStronglyTyped() => new NoneUpdate<T>();
}