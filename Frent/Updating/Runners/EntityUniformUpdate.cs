using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class EntityUniformUpdate<TComp, TUniform> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform>
{
    internal override void Run(Archetype b)
    {
        var uniform = b.World.UniformProvider.GetUniform<TUniform>();
        var chunks = Span;
        var entity = b.GetEntitySpan();

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];
            ref Chunk<Entity> eChunk = ref entity[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(eChunk[j], in uniform);
            }
        }
    }

}

[Variadic(UpdateArgFrom, UpdateArgPattern)]
[Variadic(InterfaceFrom, InterfacePattern)]
[Variadic(GetCompSpanFrom, GetCompSpanPattern)]
[Variadic(GetChunkFrom, GetChunkPattern)]
[Variadic(CallArgFrom, CallArgPattern)]
public class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform, TArg>
    where TArg : IComponent
{
    internal override void Run(Archetype b)
    {
        var uniform = b.World.UniformProvider.GetUniform<TUniform>();
        var chunks = Span;
        var entity = b.GetEntitySpan();
        var a1 = b.GetComponentSpan<TArg>();

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];
            ref Chunk<Entity> eChunk = ref entity[i];
            ref Chunk<TArg> ca = ref a1[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(eChunk[j], in uniform, ref ca[j]);
            }
        }
    }
}