using System.Runtime.CompilerServices;

namespace Frent.Collections;

internal static class InlineArrayHelper
{
    public static Span<T> AsSpan<T>(ref InlineArray2<T> array)
    {
#if NETSTANDARD2_1
        return array.AsSpan();
#else
        return array;
#endif
    }
}
