using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class UniformUpdate<TComp, TUniform> : ComponentRunnerBase<UniformUpdate<TComp, TUniform>, TComp>
    where TComp : IUniformComponent<TUniform>
{
    public override void Run(World world, Archetype b)
    {
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();
        TComp[] comps = _components;
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(uniform);
        }
    }
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
public class UniformUpdateRunnerFactory<TComp, TUniform> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUniformComponent<TUniform>
{
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class UniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<UniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    public override void Run(World world, Archetype b)
    {
        TComp[] comps = _components;
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(uniform, ref arg[i]);
        }
    }
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
[Variadic(GenArgFrom, GenArgPattern, 15)]
public class UniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform, TArg>();
}