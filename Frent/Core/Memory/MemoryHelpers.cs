using Frent.Buffers;
using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

internal static class MemoryHelpers
{
    public const int MaxComponentCount = 127;

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

    public static ImmutableArray<T> Concat<T>(ImmutableArray<T> start, ReadOnlySpan<T> span)
        where T : ITypeID
    {
        var builder = ImmutableArray.CreateBuilder<T>(start.Length + span.Length);
        for (int i = 0; i < start.Length; i++)
            builder.Add(start[i]);
        for (int i = 0; i < span.Length; i++)
        {
            var t = span[i];
            if (start.IndexOf(t) != -1)
                FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {t.Type.Name}");
            builder.Add(t);
        }
        return builder.MoveToImmutable();
    }

    public static ImmutableArray<T> Concat<T>(ImmutableArray<T> types, T type)
        where T : ITypeID
    {
        if (types.IndexOf(type) != -1)
            FrentExceptions.Throw_InvalidOperationException($"This entity already has a component of type {type.Type.Name}");

        var builder = ImmutableArray.CreateBuilder<T>(types.Length + 1);
        builder.AddRange(types);
        builder.Add(type);

        var result = builder.MoveToImmutable();
        return result;
    }

    public static ImmutableArray<T> Remove<T>(ImmutableArray<T> types, T type)
        where T : ITypeID
    {
        int index = types.IndexOf(type);
        if (index == -1)
            FrentExceptions.Throw_ComponentNotFoundException(type.Type);
        var result = types.RemoveAt(index);
        return result;
    }

    public static ImmutableArray<T> Remove<T>(ImmutableArray<T> types, ReadOnlySpan<T> span)
        where T : ITypeID
    {
        var builder = ImmutableArray.CreateBuilder<T>(types.Length);
        builder.AddRange(types);

        foreach (var type in span)
        {
            int index = builder.IndexOf(type);
            if (index == -1)
                FrentExceptions.Throw_ComponentNotFoundException(type.Type);
            builder.RemoveAt(index);
        }

        return builder.ToImmutable();
    }

    public static TValue GetOrAddNew<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new()
    {
#if NET481
        if(dictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        return dictionary[key] = new();
#else
        ref var res = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool _);
        return res ??= new();
#endif
    }
}

internal static class MemoryHelpers<T>
{
    private static ComponentArrayPool<T> _pool = new();
    internal static ArrayPool<T> Pool => _pool;
}