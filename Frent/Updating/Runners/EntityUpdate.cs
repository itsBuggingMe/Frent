using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class EntityUpdate<TComp> : ComponentRunnerBase<EntityUpdate<TComp>, TComp>
    where TComp : IEntityComponent
{
    public override void Run(World world, Archetype b)
    {
        TComp[] comp = _components;
        Span<EntityIDOnly> entities = b.GetEntitySpan()[..comp.Length];
        Entity entity = world.DefaultWorldEntity;
        for(int i = 0; i < comp.Length; i++)
        {
            entities[i].SetEntity(ref entity);
            comp[i].Update(entity);
        }
    }
    
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
public class EntityUpdateRunnerFactory<TComp> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityComponent
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUpdate<TComp, TArg> : ComponentRunnerBase<EntityUpdate<TComp, TArg>, TComp>
    where TComp : IEntityComponent<TArg>
{
    public override void Run(World world, Archetype b)
    {
        TComp[] components = _components;
        Span<EntityIDOnly> entities = b.GetEntitySpan()[..components.Length];
        Entity entity = world.DefaultWorldEntity;
        Span<TArg> arg1 = b.GetComponentSpan<TArg>()[..entities.Length];
        for(int i = 0; i < components.Length; i++)
        {
            entities[i].SetEntity(ref entity);
            components[i].Update(entity, ref arg1[i]);
        }
    }

    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b)
        => throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentRunnerFactory"/>
[Variadic(GenArgFrom, GenArgPattern, 15)]
public class EntityUpdateRunnerFactory<TComp, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp, TArg>();
}