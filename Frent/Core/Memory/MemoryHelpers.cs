using Frent.Buffers;
using Frent.Collections;
using Frent.Updating;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

internal static class MemoryHelpers
{
    public const int MaxComponentCount = 127;

#if NETSTANDARD2_1
    [ThreadStatic]
    internal static readonly ComponentHandle[] SharedTempComponentHandleBuffer = new ComponentHandle[8];
#endif

    [ThreadStatic]
    private static ComponentStorageRecord[] s_sharedTempComponentStorageBuffer = [];

    public static Span<ComponentStorageRecord> GetSharedTempComponentStorageBuffer(int minimumLength)
    {
        if(minimumLength > s_sharedTempComponentStorageBuffer.Length)
            s_sharedTempComponentStorageBuffer = new ComponentStorageRecord[minimumLength];
        return s_sharedTempComponentStorageBuffer.AsSpan(0, minimumLength);
    }

    public static uint RoundDownToPowerOfTwo(uint value) => BitOperations.RoundUpToPowerOf2((value >> 1) + 1);

    public static int RoundUpToNextMultipleOf16(int value) => (value + 15) & ~15;
    public static int RoundDownToNextMultipleOf16(int value) => value & ~15;
    public static byte BoolToByte(bool b) => Unsafe.As<bool, byte>(ref b);

    public static ref Bitset GetBitset(scoped ref Bitset[] arr, int key)
    {
        ref var bitset = ref GetValueOrResize(ref arr, key);
        return ref bitset;
    }

    public static ComponentSparseSet<T> GetSparseSet<T>(ref ComponentSparseSetBase first)
    {
        Debug.Assert(Component<T>.IsSparseComponent);
        return UnsafeExtensions.UnsafeCast<ComponentSparseSet<T>>(Unsafe.Add(ref first,
            Component<T>.SparseSetComponentIndex));
    }

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

    public static TValue GetOrAddNew<TKey, TValue>(this RefDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull, IEquatable<TKey>
        where TValue : new()
    {
        ref var res = ref dictionary.GetValueRefOrAddDefault(key, out bool _);
        return res ??= new();
    }

    public static ref T GetValueOrResize<T>(scoped ref T[] arr, int index)
    {
        if((uint)index < (uint)arr.Length)
            return ref arr[index];
        return ref ResizeAndGet(ref arr, index);
    }

    private static ref T ResizeAndGet<T>(scoped ref T[] arr, int index)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)(index + 1));
        Array.Resize(ref arr, newSize);
        return ref arr[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyBlock<TBlock>(ref byte destination, ref byte source)
        where TBlock : struct
    {
        Debug.Assert(
            typeof(TBlock) == typeof(Block2)
            || typeof(TBlock) == typeof(Block4)
            || typeof(TBlock) == typeof(Block8)
            || typeof(TBlock) == typeof(Block16));
        Unsafe.As<byte, TBlock>(ref destination) = Unsafe.As<byte, TBlock>(ref destination);
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    internal struct Block2;
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct Block4;
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct Block8;
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal struct Block16;


    // catch bugs with Unsafe.SkipInit
    [Conditional("DEBUG")]
    public static void Poison<T>(ref T item)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            return;
            //throw new NotSupportedException("Cleared anyways");

#if NET6_0_OR_GREATER
        Span<byte> raw = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref item), Unsafe.SizeOf<T>());
        raw.Fill(93);
#endif
    }
}

internal static class MemoryHelpers<T>
{
    private static ComponentArrayPool<T> _pool = new();
    internal static ArrayPool<T> Pool => _pool;
}