using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Frent.Collections;

#if NETSTANDARD2_1
internal struct InlineArray2<T>
{
    private T[] _array;

    public ref T this[int index] => ref (_array ??= new T[2])[index];
    public Span<T> Span => _array.AsSpan();
}
#elif !NET10_0_OR_GREATER
[InlineArray(2)]
internal struct InlineArray2<T>
{
    private T _element;

    [UnscopedRef] public Span<T> Span => this;
}
#endif
