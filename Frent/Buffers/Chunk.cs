using Frent.Core;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Buffers;

internal struct Chunk<TData>
{
    TData[] _buffer;
    public ref TData this[int i]
    {
        [DebuggerHidden]
        get => ref _buffer.UnsafeArrayIndex(i);
    }

    public Chunk(int len)
    {
        _buffer = MemoryHelpers<TData>.Pool.Rent(len);
    }

    public void Return()
    {
        MemoryHelpers<TData>.Pool.Return(_buffer);
        _buffer = null!;
    }

    public Span<TData> AsSpan() => _buffer;

    [DebuggerHidden]
    public Span<TData> AsSpan(int start, int length) => _buffer.AsSpan(start, length);


    public static void NextChunk(ref Chunk<TData>[] chunks, int size, int newChunkIndex)
    {
        //these arrays are too small to pool
        if(newChunkIndex == chunks.Length)
            Array.Resize(ref chunks, newChunkIndex << 1);

        var nextChunk = new Chunk<TData>(size);
        chunks[newChunkIndex] = nextChunk;
    }

    public int Length => _buffer.Length;
}
