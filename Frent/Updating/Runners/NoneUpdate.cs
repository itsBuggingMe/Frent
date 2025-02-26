using Frent.Collections;
using Frent.Core;

namespace Frent.Updating.Runners;
internal class NoneUpdate<TComp> : ComponentStorage<TComp>
{
    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) { }
    internal override void Run(World world, Archetype b) { }
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class NoneUpdateRunnerFactory<T> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<T>
{
    /// <inheritdoc/>
    public object Create() => new NoneUpdate<T>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<T>();
    ComponentStorage<T> IComponentStorageBaseFactory<T>.CreateStronglyTyped() => new NoneUpdate<T>();
}