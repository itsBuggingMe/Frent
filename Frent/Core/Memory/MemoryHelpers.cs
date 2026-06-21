using Frent.Buffers;
using Frent.Collections;
using Frent.Updating;
using Frent.Updating.Runners;
using Frent.Variadic.Generator;
using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Core;

internal static class MemoryHelpers
{
    public const int MaxComponentCount = 127;

    [ThreadStatic]
    private static ComponentStorageRecord[] s_sharedTempComponentStorageBuffer = [];

    public static Span<ComponentStorageRecord> GetSharedTempComponentStorageBuffer(int minimumLength)
    {
        if (minimumLength > s_sharedTempComponentStorageBuffer.Length)
            s_sharedTempComponentStorageBuffer = new ComponentStorageRecord[minimumLength];
        return s_sharedTempComponentStorageBuffer.AsSpan(0, minimumLength);
    }

    public static uint RoundDownToPowerOfTwo(uint value) => BitOperations.RoundUpToPowerOf2((value >> 1) + 1);

    public static int RoundUpToNextMultipleOf16(int value) => (value + 15) & ~15;
    public static int RoundDownToNextMultipleOf16(int value) => value & ~15;
    public static byte BoolToByte(bool b) => Unsafe.As<bool, byte>(ref b);

    public static bool HasDuplicateIDs<T>(ReadOnlySpan<T> ids, out T duplicate)
        where T : ITypeID, IEquatable<T>
    {
        ulong bitset = default;
        int totalIdCount =
            typeof(T) == typeof(ComponentID) ?
            Component.ComponentTable.Count :
                typeof(T) == typeof(TagID) ?
                Tag.TagTable.Count :
                    throw new NotSupportedException();

        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            ulong mask = 1UL << (id.Value & 63);

            if ((bitset & mask) != 0 && (totalIdCount <= 64 || ids.IndexOf(id) < i))
            {
                duplicate = id;
                return true;
            }

            bitset |= mask;
        }

        duplicate = default;
        return false;
    }

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

    public static int FindIndexOfRunner(UpdateMethodData[] methodMetadatas, IRunner runner)
    {
        for (int i = 0; i < methodMetadatas.Length; i++)
            if (methodMetadatas[i].Runner == runner)
                return i;
        return -1;
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
        var arrLoc = arr;
        if ((uint)index < (uint)arrLoc.Length)
            return ref arrLoc[index];
        return ref ResizeAndGet(ref arr, index);
    }

    private static ref T ResizeAndGet<T>(scoped ref T[] arr, int index)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)(index + 1));
        Array.Resize(ref arr, newSize);
        return ref arr[index];
    }


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

[Variadic("        .CompSet(Component<T>.SparseSetComponentIndex)", "|        .CompSet(Component<T$>.SparseSetComponentIndex)\n|")]
[Variadic("<T>", "<|T$, |>")]
internal static class BitsetHelper<T>
{
    public static readonly Bitset BitsetOf = new Bitset()
        .CompSet(Component<T>.SparseSetComponentIndex)
        ;
}

internal static class MemoryHelpers<T>
{
    private static ComponentArrayPool<T> _pool = new();
    internal static ArrayPool<T> Pool => _pool;
}
