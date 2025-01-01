using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class EntityUpdate<TComp> : ComponentRunnerBase<EntityUpdate<TComp>, TComp>
    where TComp : IEntityUpdateComponent
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetEntitySpan(), b.GetComponentSpan<TComp>());
    internal struct Action : ChunkHelpers<TComp>.IEntityAction
    {
        public void Run(in Entity entity, ref TComp t) => t.Update(entity);
    }
}

public class EntityUpdate<TComp, TArg> : ComponentRunnerBase<EntityUpdate<TComp, TArg>, TComp>
    where TComp : IEntityUpdateComponent<TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpanEntity<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetEntitySpan(), b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal struct Action : ChunkHelpers<TComp, TArg>.IEntityAction
    {
        public void Run(in Entity entity, ref TComp t1, ref TArg t2) => t1.Update(entity, ref t2);
    }
}