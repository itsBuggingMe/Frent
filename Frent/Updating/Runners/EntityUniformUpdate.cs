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
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp>.EnumerateComponentsWithEntity(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            new Action { Uniform = world.UniformProvider.GetUniform<TUniform>() },
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>());
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp>.EnumerateComponentsWithEntity(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            new Action { Uniform = world.UniformProvider.GetUniform<TUniform>() },
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>());
    internal record struct Action : IEntityAction<TComp>
    {
        public TUniform Uniform;
        public void Run(Entity entity, ref TComp t1) => t1.Update(entity, Uniform);
    }
}

/// <inheritdoc/>
public class EntityUniformUpdateRunnerFactory<TComp, TUniform> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformComponent<TUniform>
{
    /// <inheritdoc/>
    public object Create() => new EntityUniformUpdate<TComp, TUniform>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformComponent<TUniform, TArg>
{
    public override void Run(World world, Archetype b) =>
        ChunkHelpers<TComp, TArg>.EnumerateComponentsWithEntity(
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            new Action { Uniform = world.UniformProvider.GetUniform<TUniform>() },
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());
    public override void MultithreadedRun(CountdownEvent countdown, World world, Archetype b) =>
        MultiThreadHelpers<TComp, TArg>.EnumerateComponentsWithEntity(
            countdown,
            b.CurrentWriteChunk,
            b.LastChunkComponentCount,
            new Action { Uniform = world.UniformProvider.GetUniform<TUniform>() },
            b.GetEntitySpan(),
            b.GetComponentSpan<TComp>(),
            b.GetComponentSpan<TArg>());

    internal record struct Action : IEntityAction<TComp, TArg>
    {
        public TUniform Uniform;
        public void Run(Entity entity, ref TComp c, ref TArg t1) => c.Update(entity, Uniform, ref t1);
    }
}


[Variadic(GenArgFrom, GenArgPattern, 15)]
/// <inheritdoc/>
public class EntityUniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformComponent<TUniform, TArg>
{
    /// <inheritdoc/>
    public object Create() => new EntityUniformUpdate<TComp, TUniform, TArg>();
    /// <inheritdoc/>
    public object CreateStack() => new TrimmableStack<TComp>();
    IComponentRunner<TComp> IComponentRunnerFactory<TComp>.CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform, TArg>();
}