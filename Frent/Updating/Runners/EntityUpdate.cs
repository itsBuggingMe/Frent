using Frent.Buffers;
using Frent.Collections;
using Frent.Components;
using Frent.Core;
using Frent.Systems;
using Frent.Variadic.Generator;
using static Frent.AttributeHelpers;

namespace Frent.Updating.Runners;

internal class EntityUpdate<TComp> : ComponentStorage<TComp>
    where TComp : IEntityComponent
{
    internal override void Run(World world, Archetype b)
    {
        Span<TComp> comps = AsSpan(b.EntityCount);
        Span<EntityIDOnly> entities = b.GetEntitySpan()[..comps.Length];
        Entity entity = world.DefaultWorldEntity;
        for(int i = 0; i < comps.Length; i++)
        {
            entities[i].SetEntity(ref entity);
            comps[i].Update(entity);
        }
    }
    
    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
public class EntityUpdateRunnerFactory<TComp> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IEntityComponent
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp>();
}

[Variadic(GetSpanFrom, GetSpanPattern)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUpdate<TComp, TArg> : ComponentStorage<TComp>
    where TComp : IEntityComponent<TArg>
{
    internal override void Run(World world, Archetype b)
    {
        Span<TComp> comps = AsSpan(b.EntityCount);
        Span<EntityIDOnly> entities = b.GetEntitySpan()[..comps.Length];
        Entity entity = world.DefaultWorldEntity;
        Span<TArg> arg = b.GetComponentSpan<TArg>()[..comps.Length];
        for(int i = 0; i < comps.Length; i++)
        {
            entities[i].SetEntity(ref entity);
            comps[i].Update(entity, ref arg[i]);
        }
    }

    internal override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b)
        => throw new NotImplementedException();
}

/// <inheritdoc cref="IComponentStorageBaseFactory"/>
[Variadic(GenArgFrom, GenArgPattern)]
public class EntityUpdateRunnerFactory<TComp, TArg> : IComponentStorageBaseFactory, IComponentStorageBaseFactory<TComp>
    where TComp : IEntityComponent<TArg>
{
    /// <inheritdoc/>
    public object Create() => new EntityUpdate<TComp, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new IDTable<TComp>();
    ComponentStorage<TComp> IComponentStorageBaseFactory<TComp>.CreateStronglyTyped() => new EntityUpdate<TComp, TArg>();
}