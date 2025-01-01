using Frent.Variadic.Generator;

namespace Frent.Buffers;

[Variadic("ref T t", "|ref T$ t$, |")]
[Variadic("ChunkHelpers<T>", "ChunkHelpers<|T$, |>")]
[Variadic("Span<Chunk<T>> data1", "|Span<Chunk<T$>> data$, |")]
[Variadic("        var chunkLast1 = data1[currentChunk].AsSpan()[..lastChunkComponentCount];",
    "|        var chunkLast$ = data$[currentChunk].AsSpan()[..lastChunkComponentCount];\n|")]
[Variadic("ref chunkLast1[j]", "|ref chunkLast$[j], |")]
[Variadic("        data1 = data1[..currentChunk];", "|        data$ = data$[..currentChunk];\n|")]
[Variadic("ref comp1[j]", "|ref comp$[j], |")]
[Variadic("            var comp1 = data1[i].AsSpan()[..chunkLength];", "|            var comp$ = data$[i].AsSpan()[..chunkLength];\n|")]
[Variadic("            var comp1 = data1[i].AsSpan()[..ent.Length];", "|            var comp$ = data$[i].AsSpan()[..ent.Length];\n|")]
internal static class ChunkHelpers<T>
{
    public static void EnumerateChunkSpan<TAction>(int currentChunk, int lastChunkComponentCount, TAction action, Span<Chunk<T>> data1)
        where TAction : IAction
    {
        var chunkLast1 = data1[currentChunk].AsSpan()[..lastChunkComponentCount];

        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(ref chunkLast1[j]);
        }

        data1 = data1[..currentChunk];

        for (int i = 0; i < currentChunk; i++)
        {
            int chunkLength = data1[i].Length;
            var comp1 = data1[i].AsSpan()[..chunkLength];

            for (int j = 0; j < comp1.Length; j++)
            {
                action.Run(ref comp1[j]);
            }
        }
    }

    public static void EnumerateChunkSpanEntity<TAction>(int currentChunk, int lastChunkComponentCount, TAction action, Span<Chunk<Entity>> entityChunks, Span<Chunk<T>> data1)
        where TAction : IEntityAction
    {
        var entityLast = entityChunks[currentChunk].AsSpan()[..lastChunkComponentCount];
        var chunkLast1 = data1[currentChunk].AsSpan()[..lastChunkComponentCount];

        for (int j = 0; j < chunkLast1.Length; j++)
        {
            action.Run(entityLast[j], ref chunkLast1[j]);
        }

        entityChunks = entityChunks[..currentChunk];
        data1 = data1[..currentChunk];

        for (int i = 0; i < currentChunk; i++)
        {
            var ent = entityChunks[i].AsSpan();
            var comp1 = data1[i].AsSpan()[..ent.Length];

            for (int j = 0; j < ent.Length; j++)
            {
                action.Run(in ent[j], ref comp1[j]);
            }
        }
    }

    internal interface IAction
    {
        public void Run(ref T t);
    }

    internal interface IEntityAction
    {
        public void Run(in Entity entity, ref T t);
    }
}
