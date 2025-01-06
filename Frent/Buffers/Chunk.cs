using Frent.Core;
using System.Diagnostics;
using System.Numerics;

namespace Frent.Buffers;

internal struct Chunk<TData>
{
    TData[] _buffer;
    public ref TData this[int i]
    {
        [DebuggerHidden]
        get => ref _buffer[i];
    }

    public Chunk(int len)
    {
        _buffer = PreformanceHelpers<TData>.Pool.Rent(len);
    }

    public Span<TData> AsSpan() => _buffer;

    [DebuggerHidden]
    public Span<TData> AsSpan(int start, int length) => _buffer.AsSpan(start, length);


    public static void NextChunk(ref Chunk<TData>[] chunks, int size, int newChunkIndex)
    {
        if(BitOperations.IsPow2(newChunkIndex))
            Array.Resize(ref chunks, chunks.Length << 1);

        var nextChunk = new Chunk<TData>(size);
        chunks[newChunkIndex] = nextChunk;
    }

    public int Length => _buffer.Length;
}