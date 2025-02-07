using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class EntityUniformUpdate<TComp, TUniform> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform>, TComp>
    where TComp : IEntityUniformComponent<TUniform>
{
    public override void Run(World world, Archetype b)
    {
        Span<TComp> comps = AsSpan(b.EntityCount);
        Entity entity = world.DefaultWorldEntity;
        Span<EntityIDOnly> entityIds = b.GetEntitySpan()[..comps.Length];
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();

        for(int i = 0; i < comps.Length; i++)
        {
            entityIds[i].SetEntity(ref entity);
            comps[i].Update(entity, uniform);
        }
    }
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
public class EntityUniformUpdateRunnerFactory<TComp, TUniform> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformComponent<TUniform>
{
    /// <inheritdoc/>
    public object Create() => new EntityUniformUpdate<TComp, TUniform>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform>();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformComponent<TUniform, TArg>
{
    public override void Run(World world, Archetype b)
    {
        Entity entity = world.DefaultWorldEntity;
        Span<TComp> comps = AsSpan(b.EntityCount);
        Span<EntityIDOnly> entities = b.GetEntitySpan()[..comps.Length];
        TUniform uniform = world.UniformProvider.GetUniform<TUniform>();
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            entities[i].SetEntity(ref entity);
            comps[i].Update(entity, uniform, ref arg[i]);
        }
    }
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}


/// <inheritdoc cref="IComponentRunnerFactory"/>
[Variadic(GenArgFrom, GenArgPattern, 15)]
public class EntityUniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformComponent<TUniform, TArg>
{
    /// <inheritdoc/>
    public object Create() => new EntityUniformUpdate<TComp, TUniform, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform, TArg>();
}