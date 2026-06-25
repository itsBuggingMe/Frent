using System.Diagnostics.CodeAnalysis;
using Frent.Core;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

/// <remarks>
/// supports ids (0,  <see cref="ushort.MaxValue"/>]
/// </remarks>
internal class ShortSparseSet<T>
{
    private const ushort Tombstone = 0;
    private const int InitialCapacity = 4;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="ShortSparseSet{T}"/>.
    /// </summary>
    public int Count => _nextIndex - 1;

    private int _nextIndex = 1;

    private T[] _dense;
    private ushort[] _sparse;
    private ushort[] _ids;

    private const string INVALID_ID = "ID not in sparse set!";

    public ref T this[ushort id]
    {
        get
        {
            ref ushort index = ref EnsureSparseCapacityAndGetIndex(id);

            if (index == Tombstone)
            {
                if (_nextIndex > ushort.MaxValue)
                    FrentExceptions.Throw_InvalidOperationException($"Exceeded maximum count");

                index = (ushort)_nextIndex++;
                MemoryHelpers.GetValueOrResize(ref _ids, index) = id;
            }

            return ref MemoryHelpers.GetValueOrResize(ref _dense, index);
        }
    }

    public ShortSparseSet()
    {
        _dense = new T[InitialCapacity];
        _sparse = new ushort[InitialCapacity];
        _ids = new ushort[InitialCapacity];
    }

    public ref T Get(ushort id)
    {
        if (!TryGetIndex(id, out ushort index))
            FrentExceptions.Throw_ArgumentOutOfRangeException(INVALID_ID);

        return ref _dense[index];
    }

    public bool TryGet(ushort id, [MaybeNullWhen(false)] out T value)
    {
        if (TryGetIndex(id, out ushort index))
        {
            value = _dense[index];
            return true;
        }

        value = default;
        return false;
    }

    public void Remove(ushort id)
    {
        if (id == Tombstone)
            return;

        var sparse = _sparse;
        if (!((uint)id < (uint)sparse.Length))
            return;

        ref ushort denseIndexRef = ref sparse[id];
        ushort denseIndex = denseIndexRef;
        if (denseIndex == Tombstone || denseIndex >= _nextIndex)
            return;

        int lastIndex = --_nextIndex;
        ref T removed = ref _dense[denseIndex];

        if (denseIndex != lastIndex)
        {
            ushort movedId = _ids[lastIndex];
            removed = _dense[lastIndex];
            _ids[denseIndex] = movedId;
            _sparse[movedId] = denseIndex;
        }

        denseIndexRef = Tombstone;
        _ids[lastIndex] = Tombstone;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _dense[lastIndex] = default!;
    }

    /// <summary>
    /// Note: this span will become invalid on resize or add
    /// </summary>
    public Span<T> AsSpan() => _dense.AsSpan(1, Count);

    public void Clear()
    {
        _nextIndex = 1;
        _sparse.AsSpan().Clear();
        _ids.AsSpan().Clear();

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _dense.AsSpan(1).Clear();
    }

    private bool TryGetIndex(ushort id, out ushort index)
    {
        var sparse = _sparse;
        if (id != Tombstone && (uint)id < (uint)sparse.Length)
        {
            index = sparse[id];
            return index != Tombstone && index < _nextIndex;
        }

        index = default;
        return false;
    }

    private ref ushort EnsureSparseCapacityAndGetIndex(ushort id)
    {
        if (id == Tombstone)
            FrentExceptions.Throw_ArgumentOutOfRangeException(INVALID_ID);

        return ref MemoryHelpers.GetValueOrResize(ref _sparse, id);
    }
}
