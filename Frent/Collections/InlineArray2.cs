using System.Runtime.CompilerServices;

namespace Frent.Collections;

#if NETSTANDARD2_1
internal struct InlineArray2<T>
{
    private T[] _array;

    public ref T this[int index] => ref (_array ??= new T[2])[index];
    public Span<T> AsSpan() => (_array ??= new T[2]);
}
#elif !NET10_0_OR_GREATER
[InlineArray(2)]
internal struct InlineArray2<T>
{
    private T _element;
}
#endif
