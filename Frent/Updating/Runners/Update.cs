using Frent.Buffers;
using Frent.Core;
using Frent.Components;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IUpdateComponent
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>());
    internal struct Action : ChunkHelpers<TComp>.IAction
    {
        public void Run(ref TComp t) => t.Update();
    }
}

public class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IUpdateComponent<TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal struct Action : ChunkHelpers<TComp, TArg>.IAction
    {
        public void Run(ref TComp t1, ref TArg t2) => t1.Update(ref t2);
    }
}