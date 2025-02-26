using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class UniformUpdate<TComp, TUniform> : ComponentStorage<TComp>
    where TComp : IUniformComponent<TUniform>
{
    internal override void Run(World world, Archetype b)
    {
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();
        Span<TComp> comps = AsSpan(b.EntityCount);
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(uniform);
        }
    }
    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class UniformUpdateRunnerFactory<TComp, TUniform> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IUniformComponent<TUniform>
{
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform>();
}

[Variadic(GetSpanFrom, GetSpanPattern)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class UniformUpdate<TComp, TUniform, TArg> : ComponentStorage<TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    internal override void Run(World world, Archetype b)
    {
        Span<TComp> comps = AsSpan(b.EntityCount);
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            comps[i].Update(uniform, ref arg[i]);
        }
    }
    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(GenArgFrom, GenArgPattern)]
public class UniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IUniformComponent<TUniform, TArg>
{
    /// <inheritdoc/>
    public object Create() => new UniformUpdate<TComp, TUniform, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new UniformUpdate<TComp, TUniform, TArg>();
}