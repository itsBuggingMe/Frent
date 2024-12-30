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
    internal override void Run(Archetype b)
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
public class Update<TComp, TArg> : ComponentRunnerBase<Update<TComp, TArg>, TComp>
    where TComp : IUpdateComponent<TArg>
    where TArg : IComponent
{
    internal override void Run(Archetype b)
    {
        var thisCompSpan = Span;
        var a1 = b.GetComponentSpan<TArg>();

        Debug.Assert(a1.Length == thisCompSpan.Length);

        for(int i = 0; i < thisCompSpan.Length; i++)
        {
            ref Chunk<TComp> chunk = ref thisCompSpan[i];
            ref Chunk<TArg> ca = ref a1[i];

            for(int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(ref ca[j]);
            }
        }
    }
}