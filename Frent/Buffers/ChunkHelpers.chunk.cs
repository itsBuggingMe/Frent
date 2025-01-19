using Frent.Systems;
using Frent.Variadic.Generator;

namespace Frent.Buffers;

[Variadic("Span<Chunk<TArg>> data1", "|Span<Chunk<TArg$>> data$, |")]
[Variadic("        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];", 
    "|        var chunkLast$ = data$[curChk].AsSpan()[..lastChkCompCount];\n|")]
[Variadic("        data1 = data1[..curChk];", "|        data$ = data$[..curChk];\n|")]
[Variadic("ref chunkLast1[j]", "|ref chunkLast$[j], |")]
[Variadic("data1[i].AsSpan()", "|data$[i].AsSpan(), |")]
[Variadic("Span<TArg> arg1", "|Span<TArg$> arg$, |")]
[Variadic("ref arg1[i]", "|ref arg$[i], |")]
[Variadic("            arg1 = arg1[..arg1.Length];", "|            arg$ = arg$[..arg1.Length];\n|")]
[Variadic("data1);", "|data$, |);\n")]
[Variadic("TArg>", "|TArg$, |>")]
partial class ChunkHelpers<TArg>
{
    public static void EnumerateChunks<TChunkAction, TAction>(int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<TArg>> data1)
        where TChunkAction : struct, IChunkAction<TArg>
        where TAction : struct, IAction<TArg>
    {
        //AsSpan()[..n] is better than AsSpan(0, n) since the jit only recognises the span slice itself
        //Code side is also smaller
        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];

        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(ref chunkLast1[j]);
        }

        data1 = data1[..curChk];

        for (int i = 0; i < curChk; i++)
        {
            chunk.RunChunk(data1[i].AsSpan());
        }
    }

    public static void EnumerateComponents<TAction>(int curChk, int lastChkCompCount, TAction action, Span<Chunk<TArg>> data1)
        where TAction : struct, IAction<TArg>
        => EnumerateChunks(curChk, lastChkCompCount, new OnEachChunkAction<TAction>(action), action, data1);

    private struct OnEachChunkAction<TInnerAction>(TInnerAction action) : IChunkAction<TArg>
        where TInnerAction : struct, IAction<TArg>
    {
        public void RunChunk(Span<TArg> arg1)
        {
            arg1 = arg1[..arg1.Length];

            for(int i = 0; i < arg1.Length; i++)
            {
                action.Run(ref arg1[i]);
            }
        }
    }

    public static void EnumerateChunksWithEntity<TChunkAction, TAction>(int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<Entity>> entities, Span<Chunk<TArg>> data1)
        where TChunkAction : struct, IEntityChunkAction<TArg>
        where TAction : struct, IEntityAction<TArg>
    {
        var entLast1 = entities[curChk].AsSpan()[..lastChkCompCount];
        var chunkLast1 = data1[curChk].AsSpan()[..lastChkCompCount];

        for (int j = 0; j < entLast1.Length; j++)
        {
            action.Run(entLast1[j], ref chunkLast1[j]);
        }

        entities = entities[..curChk];
        data1 = data1[..curChk];

        for (int i = 0; i < curChk; i++)
        {
            chunk.RunChunk(entities[i].AsSpan(), data1[i].AsSpan());
        }
    }

    public static void EnumerateComponentsWithEntity<TAction>(int curChk, int lastChkCompCount, TAction action, Span<Chunk<Entity>> entities, Span<Chunk<TArg>> data1)
        where TAction : struct, IEntityAction<TArg>
        => EnumerateChunksWithEntity(curChk, lastChkCompCount, new OnEachChunkWithEntityAction<TAction>(action), action, entities, data1);

    private struct OnEachChunkWithEntityAction<TInnerAction>(TInnerAction action) : IEntityChunkAction<TArg>
        where TInnerAction : struct, IEntityAction<TArg>
    {
        public void RunChunk(ReadOnlySpan<Entity> entities, Span<TArg> arg1)
        {
            arg1 = arg1[..entities.Length];

            for (int i = 0; i < entities.Length; i++)
            {
                action.Run(entities[i], ref arg1[i]);
            }
        }
    }
}

partial class ChunkHelpers
{
    public static void EnumerateChunksWithEntity<TChunkAction, TAction>(int curChk, int lastChkCompCount, TChunkAction chunk, TAction action, Span<Chunk<Entity>> entities)
        where TChunkAction : struct, IEntityChunkAction
        where TAction : struct, IEntityAction
    {
        //AsSpan()[..n] is better than AsSpan(0, n) since the jit only recognises the span slice itself
        //Code side is also smaller
        var chunkLast1 = entities[curChk].AsSpan()[..lastChkCompCount];

        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(chunkLast1[j]);
        }

        for (int i = 0; i < entities.Length; i++)
        {
            chunk.RunChunk(entities[i].AsSpan());
        }
    }

    public static void EnumerateComponentsWithEntity<TAction>(int curChk, int lastChkCompCount, TAction action, Span<Chunk<Entity>> entities)
        where TAction : struct, IEntityAction
        => EnumerateChunksWithEntity(curChk, lastChkCompCount, new OnEachChunkAction<TAction>(action), action, entities);

    private struct OnEachChunkAction<TInnerAction>(TInnerAction action) : IEntityChunkAction
        where TInnerAction : struct, IEntityAction
    {
        public void RunChunk(ReadOnlySpan<Entity> arg1)
        {
            arg1 = arg1[..arg1.Length];

            for (int i = 0; i < arg1.Length; i++)
            {
                action.Run(arg1[i]);
            }
        }
    }
}