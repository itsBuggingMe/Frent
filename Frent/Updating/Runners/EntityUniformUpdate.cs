using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class EntityUniformUpdate<TComp, TUniform> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetEntitySpan(), b.GetComponentSpan<TComp>());
    internal record struct Action : ChunkHelpers<TComp>.IEntityAction
    {
        public TUniform Uniform;
        public void Run(in Entity entity, ref TComp t1) => t1.Update(entity, in Uniform);
    }
}

public class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform, TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, new() { Uniform = b.World.UniformProvider.GetUniform<TUniform>() }, b.GetEntitySpan(), b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal record struct Action : ChunkHelpers<TComp, TArg>.IEntityAction
    {
        public TUniform Uniform;
        public void Run(in Entity entity, ref TComp t1, ref TArg t2) => t1.Update(entity, in Uniform, ref t2);
    }
}