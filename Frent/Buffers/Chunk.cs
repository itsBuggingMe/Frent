using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Buffers;

public struct Chunk<TData>
{
    public ref TData this[int i]
    {
        [DebuggerHidden]
        get => ref _buffer[i];
    }

    const int MaxChunkSize = 8192;

    public Chunk(int len)
    {
        Debug.Assert(len > 0 && len <= MaxChunkSize);
        _buffer = new TData[len];
    }

    TData[] _buffer;
    public Span<TData> AsSpan() => _buffer;
    [DebuggerHidden]
    public Span<TData> AsSpan(int start, int length) => _buffer.AsSpan(start, length);

    public Chunk<TData> CreateNext()
    {
        int len = _buffer.Length;
        return new Chunk<TData>(len == MaxChunkSize ? MaxChunkSize : len << 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NextChunk(ref Chunk<TData>[] chunks)
    {
        var nextChunk = chunks[^1].CreateNext();
        Array.Resize(ref chunks, chunks.Length + 1);
        chunks[^1] = nextChunk;
    }

    public int Length => _buffer.Length;
}