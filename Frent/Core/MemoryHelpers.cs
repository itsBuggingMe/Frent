using Frent.Buffers;
using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

internal static class MemoryHelpers
{
    public const int MaxComponentCount = 16;
    public static int MaxArchetypeChunkSize = 16384*4;
    
    public static uint RoundDownToPowerOfTwo(uint value) => BitOperations.RoundUpToPowerOf2((value >> 1) + 1);

    public static int RoundUpToNextMultipleOf16(int value) => (value + 15) & ~15;

    public static ImmutableArray<T> ReadOnlySpanToImmutableArray<T>(ReadOnlySpan<T> span)
    {
        var builder = ImmutableArray.CreateBuilder<T>(span.Length);
        for (int i = 0; i < span.Length; i++)
            builder.Add(span[i]);
        return builder.MoveToImmutable();
    }
    public static ImmutableArray<T> ConcatImmutable<T>(ImmutableArray<T> start, ReadOnlySpan<T> span)
    {
        var builder = ImmutableArray.CreateBuilder<T>(start.Length + span.Length);
        for (int i = 0; i < start.Length; i++)
            builder.Add(start[i]);
        for (int i = 0; i < span.Length; i++)
            builder.Add(span[i]);
        return builder.MoveToImmutable();
    }

    public static int GetSizeOfType<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            //a class means at least ~68 bytes are used (assuming stuff is pretty fragmented)
            //can't really get the size of the type
            return 68;
        }

        return Marshal.SizeOf<T>();
    }
}

internal static class MemoryHelpers<T>
{
    private static ComponentArrayPool<T> _pool = new();
    internal static ArrayPool<T> Pool => _pool;
}