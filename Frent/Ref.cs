namespace Frent;

/// <summary>
/// A wrapper ref struct over a reference to a <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type this <see cref="Ref{T}"/> wraps over</typeparam>
/// <param name="comp">The reference to wrap</param>
public ref struct Ref<T>
{
#if NET7_0_OR_GREATER
    private ref T _component;

    public static Ref<T> CreateRef(Span<T> values, int index)
    {
        return new Ref<T>()
        {
            _component = ref values[index]
        };
    }

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public readonly ref T Component => ref _component;
    public override readonly string ToString() => _component?.ToString() ?? "null";
#else
    private Span<T> _component;

    public static Ref<T> CreateRef(Span<T> values, int index)
    {
        return new Ref<T>()
        {
            _component = values.Slice(index, 1),
        };
    }

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public readonly ref T Component => ref _component[0];
    public override readonly string ToString() => (_component.Length == 0 ? null : _component[0]?.ToString()) ?? "null";
#endif

    public static implicit operator T(Ref<T> @ref) => @ref.Component;
}
