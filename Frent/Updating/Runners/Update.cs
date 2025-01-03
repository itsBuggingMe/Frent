using Frent.Buffers;
using Frent.Core;
using Frent.Components;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;
using System;

namespace Frent.Updating.Runners;

public class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IUpdateComponent
{
    public override void Run(Archetype b) => ChunkHelpers<TComp>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>());
    internal struct Action : IQuery<TComp>
    {
        public void Run(ref TComp t) => t.Update();
    }
}

[Variadic(GetSpanFrom, GetSpanPattern, 15)]
[Variadic(GenArgFrom, GenArgPattern)]
[Variadic(GetArgFrom, GetArgPattern)]
[Variadic(PutArgFrom, PutArgPattern)]
public class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IUpdateComponent<TArg>
{
    public override void Run(Archetype b) => ChunkHelpers<TComp, TArg>.EnumerateChunkSpan<Action>(b.CurrentWriteChunk, b.LastChunkComponentCount, default, b.GetComponentSpan<TComp>(), b.GetComponentSpan<TArg>());
    internal struct Action : IQuery<TComp, TArg>
    {
        public void Run(ref TComp c, ref TArg t1) => c.Update(ref t1);
    }
}