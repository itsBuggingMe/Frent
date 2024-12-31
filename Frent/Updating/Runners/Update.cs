using Frent.Buffers;
using Frent.Components;
using System.Diagnostics;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;
using Frent.Core;

namespace Frent.Updating.Runners;

public class Update<TComp> : ComponentRunnerBase<Update<TComp>, TComp>
    where TComp : IUpdateComponent
{
    public override void Run(Archetype b)
    {
        foreach (var chunk in Span)
            foreach (var t in chunk.AsSpan())
                t.Update();
    }
}


[Variadic(UpdateArgFrom, UpdateArgPattern)]
[Variadic(InterfaceFrom, InterfacePattern)]
[Variadic(GetCompSpanFrom, GetCompSpanPattern)]
[Variadic(GetChunkFrom, GetChunkPattern)]
[Variadic(CallArgFrom, CallArgPattern)]
[Variadic(GetChunkLastFrom, GetChunkLastPattern)]
[Variadic(CallArgLastFrom, CallArgLastPattern)]
public class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IUpdateComponent<TArg>
{
    public override void Run(Archetype b)
    {
        var chunks = Span;
        var a1 = b.GetComponentSpan<TArg>();

        Debug.Assert(a1.Length == chunks.Length);

        for(int i = 0; i < b.ChunkCount; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];
            ref Chunk<TArg> ca = ref a1[i];

            for(int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(ref ca[j]);
            }
        }

        var chunkLast = chunks[^1].AsSpan(0, b.LastChunkComponentCount);
        var caLast = a1[..(b.ChunkCount + 1)][^1].AsSpan(0, b.LastChunkComponentCount);

        for (int j = 0; j < chunkLast.Length; j++)
        {
            chunkLast[j].Update(ref caLast[j]);
        }
    }
}