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
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp>.EnumerateComponentsWithEntity(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>());
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp>.EnumerateComponentsWithEntity(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>());
    internal struct Action : IEntityAction<TComp>
    {
        public void Run(Entity entity, ref TComp t) => t.Update(entity);
    }
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
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp, TArg>.EnumerateComponentsWithEntity(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp, TArg>.EnumerateComponentsWithEntity(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            default(Action),
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());
    internal struct Action : IEntityAction<TComp, TArg>
    {
        public void Run(Entity entity, ref TComp c, ref TArg t1) => c.Update(entity, ref t1);
    }
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