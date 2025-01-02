using Frent.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Frent.Buffers;

public struct Chunk<TData>
{
    TData[] _buffer;
    public ref TData this[int i]
    {
        [DebuggerHidden]
        get => ref _buffer[i];
    }

    public Chunk(int len)
    {
        _buffer = new TData[len];
    }

    public Span<TData> AsSpan() => _buffer;
    [DebuggerHidden]
    public Span<TData> AsSpan(int start, int length) => _buffer.AsSpan(start, length);


    public static void NextChunk(ref Chunk<TData>[] chunks, int size)
    {
        var nextChunk = new Chunk<TData>(size);
        Array.Resize(ref chunks, chunks.Length + 1);
        chunks[^1] = nextChunk;
    }

    public int Length => _buffer.Length;
}