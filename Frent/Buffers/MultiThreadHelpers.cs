using Frent.Systems;
using Frent.Variadic.Generator;

namespace Frent.Buffers;

[Variadic("Span<Chunk<TArg>> data1", "|Span<Chunk<TArg$>> data$, |")]
[Variadic("        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];",
    "|        var chunkLast$ = data$[curChk].AsSpan()[..lastChkCompCount];\n|")]
[Variadic("ref chunkLast1[j]", "|ref chunkLast$[j], |")]
[Variadic("data1);", "|data$, |);\n")]
[Variadic("Chunk<TArg> data,", "|Chunk<TArg$> data$, |,")]
[Variadic("data1[i]", "|data$[i], |")]
[Variadic("data.AsSpan()", "|data$.AsSpan(), |")]
[Variadic("TArg>", "|TArg$, |>")]
partial class MultiThreadHelpers<TArg>
{
    public static void EnumerateChunks<TChunkAction, TAction>(CountdownEvent countdown, int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<TArg>> data1)
        where TChunkAction : struct, IChunkAction<TArg>
        where TAction : struct, IAction<TArg>
    {
        countdown.Reset(curChk);

        for (int i = 0; i < curChk; i++)
        {
            ThreadPool.UnsafeQueueUserWorkItem(c => c.Execute(), new ActionState<TChunkAction>(countdown, data1[i], chunk), true);//TODO: benchmark this parameter
        }

        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];
        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(ref chunkLast1[j]);
        }

        countdown.Wait();
    }

    internal struct ActionState<TAction>(CountdownEvent counter, Chunk<TArg> data, TAction action)
        where TAction : struct, IChunkAction<TArg>
    {
        public void Execute()
        {
            try
            {
                action.RunChunk(data.AsSpan());
            }
            finally
            {
                counter.Signal();
            }
        }
    }

    public static void EnumerateComponents<TAction>(CountdownEvent countdown, int curChk, int lastChkCompCount, TAction action, Span<Chunk<TArg>> data1)
        where TAction : struct, IAction<TArg>
        => EnumerateChunks(countdown, curChk, lastChkCompCount, new ChunkHelpers<TArg>.OnEachChunkAction<TAction>(action), action, data1);

    public static void EnumerateChunksWithEntity<TChunkAction, TAction>(CountdownEvent countdown, int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<Entity>> entities, Span<Chunk<TArg>> data1)
        where TChunkAction : struct, IEntityChunkAction<TArg>
        where TAction : struct, IEntityAction<TArg>
    {
        countdown.Reset(curChk);

        for (int i = 0; i < curChk; i++)
        {
            ThreadPool.UnsafeQueueUserWorkItem(c => c.Execute(), new EntityActionState<TChunkAction>(countdown, entities[i], data1[i], chunk), true);//TODO: benchmark this parameter
        }

        var entLast = entities[curChk].AsSpan()[..lastChkCompCount];
        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];
        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(entLast[j], ref chunkLast1[j]);
        }

        countdown.Wait();
    }

    internal struct EntityActionState<TAction>(CountdownEvent counter, Chunk<Entity> entitites, Chunk<TArg> data, TAction action)
        where TAction : struct, IEntityChunkAction<TArg>
    {
        public void Execute()
        {
            try
            {
                action.RunChunk(entitites.AsSpan(), data.AsSpan());
            }
            finally
            {
                counter.Signal();
            }
        }
    }

    public static void EnumerateComponentsWithEntity<TAction>(CountdownEvent countdown, int curChk, int lastChkCompCount, TAction action, Span<Chunk<Entity>> entitites, Span<Chunk<TArg>> data1)
        where TAction : struct, IEntityAction<TArg>
        => EnumerateChunksWithEntity(countdown, curChk, lastChkCompCount, new ChunkHelpers<TArg>.OnEachChunkWithEntityAction<TAction>(action), action, entitites, data1);
}

partial class MultiThreadHelpers
{
    public static void EnumerateChunksWithEntity<TChunkAction, TAction>(CountdownEvent countdown, int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<Entity>> entities)
        where TAction : IEntityAction
        where TChunkAction : IEntityChunkAction
    {
        countdown.Reset(curChk);

        for (int i = 0; i < curChk; i++)
        {
            ThreadPool.UnsafeQueueUserWorkItem(c => c.Execute(), new EntityActionState<TChunkAction>(countdown, entities[i], chunk), true);//TODO: benchmark this parameter
        }

        var entLast = entities[curChk].AsSpan()[..lastChkCompCount];

        for (int j = 0; j < entLast.Length; j++)
        {
            action.Run(entLast[j]);
        }

        countdown.Wait();
    }

    internal struct EntityActionState<TChunkAction>(CountdownEvent counter, Chunk<Entity> entities, TChunkAction action)
        where TChunkAction : IEntityChunkAction
    {
        public void Execute()
        {
            try
            {
                action.RunChunk(entities.AsSpan());
            }
            finally
            {
                counter.Signal();
            }
        }
    }

    public static void EnumerateComponentsWithEntity<TAction>(CountdownEvent counter, int curChk, int lastChkCompCount, TAction action, Span<Chunk<Entity>> entities)
        where TAction : struct, IEntityAction
        => EnumerateChunksWithEntity(counter, curChk, lastChkCompCount, new ChunkHelpers.OnEachChunkAction<TAction>(action), action, entities);
}
