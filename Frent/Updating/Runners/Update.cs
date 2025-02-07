using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IComponent
{
    public override void Run(World world, Archetype b)
    {
        Span<TComp> arr = _components.AsSpan(0, b.EntityCount);
        for(int i = 0; i < arr.Length; i++)
        {
            arr[i].Update();
        }
    }

    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
public class UpdateRunnerFactory<TComp> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IComponent
{
    /// <inheritdoc/>
    public object Create() => new Update<TComp>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IComponent<TArg>
{
    public override void Run(World world, Archetype b)
    {
        Span<TComp> comps = _components.AsSpan(0, b.EntityCount);
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(ref arg[i]);
        }    
    }

    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
[Variadic(GenArgFrom, GenArgPattern, 15)]
public class UpdateRunnerFactory<TComp, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new Update<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new Update<TComp, TArg>();
}