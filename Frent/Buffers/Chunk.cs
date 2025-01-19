using Frent.Core;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Buffers;

internal struct Chunk<TData>
{
    internal TData[] Buffer;
    public ref TData this[int i]
    {
        [DebuggerHidden]
        get => ref Buffer.UnsafeArrayIndex(i);
    }

    public Chunk(int len)
    {
        Buffer = MemoryHelpers<TData>.Pool.Rent(len);
    }

    public void Return()
    {
        MemoryHelpers<TData>.Pool.Return(Buffer);
        Buffer = null!;
    }

    public Span<TData> AsSpan() => Buffer;

    [DebuggerHidden]
    public Span<TData> AsSpan(int start, int length) => Buffer.AsSpan(start, length);


    public static void NextChunk(ref Chunk<TData>[] chunks, int size, int newChunkIndex)
    {
        //these arrays are too small to pool
        if(newChunkIndex == chunks.Length)
            Array.Resize(ref chunks, newChunkIndex << 1);

        var nextChunk = new Chunk<TData>(size);
        chunks[newChunkIndex] = nextChunk;
    }

    public int Length => Buffer.Length;
}
