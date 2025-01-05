using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

internal class EntityUniformUpdate<TComp, TUniform> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetEntitySpan(), b.GetComponentSpan<TComp>());
    internal record struct Action : IQueryEntity<TComp>
    {
        public TUniform Uniform;
        public void Run(Entity entity, ref TComp t1) => t1.Update(entity, in Uniform);
    }
}

public class EntityUniformUpdateRunnerFactory<TComp, TUniform> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform>
{
    public object Create() => new EntityUniformUpdate<TComp, TUniform>();
    public IComponentRunner<TComp> CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform>();
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
internal class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform, TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetEntitySpan(), b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal record struct Action : IQueryEntity<TComp, TArg>
    {
        public TUniform Uniform;
        public void Run(Entity entity, ref TComp c, ref TArg t1) => c.Update(entity, in Uniform, ref t1);
    }
}

[Variadic(GenArgFrom, GenArgPattern, 15)]
public class EntityUniformUpdateRunnerFactory<TComp, TUniform, TArg> : IComponentRunnerFactory, IComponentRunnerFactory<TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform, TArg>
{
    public object Create() => new EntityUniformUpdate<TComp, TUniform, TArg>();
    public IComponentRunner<TComp> CreateStronglyTyped() => new EntityUniformUpdate<TComp, TUniform, TArg>();
}