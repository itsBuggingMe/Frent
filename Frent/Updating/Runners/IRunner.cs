using Frent.Collections;
using Frent.Core;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Updating.Runners;

/// <inheritdoc cref="GenerationServices"/>
public interface IRunner
{
    internal void RunArchetypical(Array buffer, Archetype b, World world, int start, int length);
    internal void RunSparse(ComponentSparseSetBase sparseSet, World world, Span<int> idsToUpdate);
    internal static ref T GetComponentStorageDataReference<T>(Array array)
    {
        return ref MemoryMarshal.GetArrayDataReference(UnsafeExtensions.UnsafeCast<T[]>(array));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T InitSparse<T>(ref ComponentSparseSetBase first, out Span<int> sparseArgArray)
    {
        if (Component<T>.IsSparseComponent)
        {
            ComponentSparseSet<T> argSparseSet = MemoryHelpers.GetSparseSet<T>(ref first);
            sparseArgArray = argSparseSet.SparseSpan();
            return ref argSparseSet.GetComponentDataReference();
        }

        sparseArgArray = default;
        return ref Unsafe.NullRef<T>();
    }
}