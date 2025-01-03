namespace Frent;

/// <summary>
/// A wrapper ref struct over a reference to a <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type this <see cref="Ref{T}"/> wraps over</typeparam>
/// <param name="comp">The reference to wrap</param>
public ref struct Ref<T>
{
#if NET7_0_OR_GREATER
    private Ref(ref T comp) => _comp = ref comp;

    private ref T _comp;

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public ref T Component => ref _comp;
    public static implicit operator T(Ref<T> @ref) => @ref.Component;
    public override string ToString() => Component?.ToString() ?? "null";
    internal static Ref<T> Create(Span<T> span, int index) => new Ref<T>(ref span[index]);
#else
    private Ref(Span<T> comp) => _comp = comp;

    private Span<T> _comp;

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public ref T Component => ref _comp[0];
    public static implicit operator T(Ref<T> @ref) => @ref.Component;
    public override string ToString() => Component?.ToString() ?? "null";
    internal static Ref<T> Create(Span<T> span, int index) => new Ref<T>(span.Slice(index, 1));
#endif
}
