using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using System;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class UniformUpdate<TComp, TUniform> : ComponentRunnerBase<UniformUpdate<TComp, TUniform>, TComp>
    where TComp : IUniformUpdateComponent<TUniform>
{
    internal override void Run(Archetype b)
    {
        var uniform = b.World.UniformProvider.GetUniform<TUniform>();
        var chunks = Span;

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(in uniform);
            }
        }
    }
}

[Variadic(UpdateArgFrom, UpdateArgPattern)]
[Variadic(InterfaceFrom, InterfacePattern)]
[Variadic(GetCompSpanFrom, GetCompSpanPattern)]
[Variadic(GetChunkFrom, GetChunkPattern)]
[Variadic(CallArgFrom, CallArgPattern)]
public class UniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<UniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IUniformUpdateComponent<TUniform, TArg>
    where TArg : IComponent
{
    internal override void Run(Archetype b)
    {
        var uniform = b.World.UniformProvider.GetUniform<TUniform>();
        var chunks = Span;
        var a1 = b.GetComponentSpan<TArg>();

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];
            ref Chunk<TArg> ca = ref a1[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(in uniform, ref ca[j]);
            }
        }
    }
}