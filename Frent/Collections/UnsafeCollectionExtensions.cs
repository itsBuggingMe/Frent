using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frent.Collections;

internal static class UnsafeCollectionExtensions
{
    //no actual unsafe code please

#if DEBUG
    public static ref T UnsafeArrayIndex<T>(this T[] array, int index)
    {
        ref T start = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref start, index);
    }

    public static ref T UnsafeSpanIndex<T>(this Span<T> span, int index)
    {
        ref T start = ref MemoryMarshal.GetReference(span);
        return ref Unsafe.Add(ref start, index);
    }
#endif
}
