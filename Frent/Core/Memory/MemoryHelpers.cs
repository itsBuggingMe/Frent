using Frent.Buffers;
using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Frent.Core;

internal static class MemoryHelpers
{
    public const int MaxComponentCount = 16;
    public static int MaxArchetypeChunkSize = 16384 * 4;

    public static uint RoundDownToPowerOfTwo(uint value) => BitOperations.RoundUpToPowerOf2((value >> 1) + 1);

    public static int RoundUpToNextMultipleOf16(int value) => (value + 15) & ~15;
    public static int RoundDownToNextMultipleOf16(int value) => value & ~15;
    public static byte BoolToByte(bool b) => Unsafe.As<bool, byte>(ref b);
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

    public static ReadOnlySpan<T> Concat<T>(ImmutableArray<T> types, T type, out ImmutableArray<T> result)
        where T : ITypeID
    {
        if (types.IndexOf(type) != -1)
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {type.Type.Name}");

        var builder = ImmutableArray.CreateBuilder<T>(types.Length + 1);
        builder.AddRange(types);
        builder.Add(type);

        result = builder.MoveToImmutable();
        return result.AsSpan();
    }

    public static ReadOnlySpan<T> Remove<T>(ImmutableArray<T> types, T type, out ImmutableArray<T> result)
        where T : ITypeID
    {
        int index = types.IndexOf(type);
        if (index == -1)
            FrentExceptions.Throw_ComponentNotFoundException(type.Type);
        result = types.RemoveAt(index);
        return result.AsSpan();
    }
}

internal static class MemoryHelpers<T>
{
    private static ComponentArrayPool<T> _pool = new();
    internal static ArrayPool<T> Pool => _pool;
}