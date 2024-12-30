using Frent.Buffers;
using Frent.Components;
using Frent.Core;
using Frent.Variadic.Generator;
using static Frent.Updating.Variadics;

namespace Frent.Updating.Runners;

public class EntityUpdate<TComp> : ComponentRunnerBase<EntityUpdate<TComp>, TComp>
    where TComp : IEntityUpdateComponent
{
    public override void Run(Archetype b)
    {
        var chunks = Span;

        var entity = b.GetEntitySpan();

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<Entity> eChunk = ref entity[i];

            ref Chunk<TComp> chunk = ref chunks[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(eChunk[j]);
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
public class EntityUpdate<TComp, TArg> : ComponentRunnerBase<EntityUpdate<TComp, TArg>, TComp>
    where TComp : IEntityUpdateComponent<TArg>
{
    public override void Run(Archetype b)
    {
        var chunks = Span;

        var entity = b.GetEntitySpan();
        var a1 = b.GetComponentSpan<TArg>();

        for (int i = 0; i < chunks.Length; i++)
        {
            ref Chunk<Entity> eChunk = ref entity[i];

            ref Chunk<TComp> chunk = ref chunks[i];

            ref Chunk<TArg> ca = ref a1[i];

            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j].Update(eChunk[j], ref ca[j]);
            }
        }

        var chunkLast = chunks[^1].AsSpan(0, b.LastChunkComponentCount);
        var entityLast = entity[^1].AsSpan(0, b.LastChunkComponentCount);

        var caLast = a1[^1].AsSpan(0, b.LastChunkComponentCount);

        for (int j = 0; j < chunkLast.Length; j++)
        {
            chunkLast[j].Update(entityLast[j], ref caLast[j]);
        }
    }
}