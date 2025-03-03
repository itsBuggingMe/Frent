using System.Runtime.CompilerServices;

namespace Frent.Collections;

internal struct InlineArray8<T>
{
    public T _0;
    public T _1;
    public T _2;
    public T _3;
    public T _4;
    public T _5;
    public T _6;
    public T _7;

    public static ref T Get(ref InlineArray8<T> array, int index)
    {
        return ref Unsafe.Add(ref array._0, index);
    }
}