using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class EntityUniformUpdate<TComp, TUniform> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform>
{
    public override void Run(Archetype b)
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
[Variadic(GetChunkLastFrom, GetChunkLastPattern)]
[Variadic(CallArgLastFrom, CallArgLastPattern)]
public class EntityUniformUpdate<TComp, TUniform, TArg> : ComponentRunnerBase<EntityUniformUpdate<TComp, TUniform, TArg>, TComp>
    where TComp : IEntityUniformUpdateComponent<TUniform, TArg>
{
    public override void Run(Archetype b)
    {
        var uniform = b.World.UniformProvider.GetUniform<TUniform>();
        var chunks = Span;
        var entity = b.GetEntitySpan();
        var a1 = b.GetComponentSpan<TArg>();

        for (int i = 0; i < b.ChunkCount; i++)
        {
            ref Chunk<TComp> chunk = ref chunks[i];
            ref Chunk<Entity> eChunk = ref entity[i];
            ref Chunk<TArg> ca = ref a1[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(eChunk[j], in uniform, ref ca[j]);
            }
        }

        var chunkLast = chunks[^1].AsSpan(0, b.LastChunkComponentCount);
        var entityLast = entity[^1].AsSpan(0, b.LastChunkComponentCount);
        var caLast = a1[..(b.ChunkCount + 1)][^1].AsSpan(0, b.LastChunkComponentCount);

        for (int j = 0; j < chunkLast.Length; j++)
        {
            chunkLast[j].Update(entityLast[j], in uniform, ref caLast[j]);
        }
    }
}