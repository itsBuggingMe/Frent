namespace Frent.Core;

/// <summary>
/// A wrapper ref struct over a reference to a <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type this <see cref="Ref{T}"/> wraps over</typeparam>
public ref struct Ref<T>
{
#if NET7_0_OR_GREATER
    private Ref(ref T comp) => _comp = ref comp;

    private ref T _comp;

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public ref T Val => ref _comp;
    /// <summary>
    /// Extracts the wrapped <typeparamref name="T"/> from this <see cref="Ref{T}"/>
    /// </summary>
    public static implicit operator T(Ref<T> @ref) => @ref.Val;
    /// <summary>
    /// Calls the wrapped <typeparamref name="T"/>'s ToString() function, or returns null.
    /// </summary>
    /// <returns>A string representation of the wrapped <typeparamref name="T"/>'s</returns>
    public override string ToString() => Val?.ToString() ?? "null";
    internal static Ref<T> Create(Span<T> span, int index) => new Ref<T>(ref span[index]);
#else
    private Ref(Span<T> comp) => _comp = comp;

    private Span<T> _comp;

    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public ref T Val => ref _comp[0];
    /// <summary>
    /// Extracts the wrapped <typeparamref name="T"/> from this <see cref="Ref{T}"/>
    /// /// </summary>
    public static implicit operator T(Ref<T> @ref) => @ref.Val;
    /// <summary>
    /// Calls the wrapped <typeparamref name="T"/>'s ToString() function, or returns null.
    /// </summary>
    /// <returns>A string representation of the wrapped <typeparamref name="T"/>'s</returns>
    public override string ToString() => Val?.ToString() ?? "null";
    internal static Ref<T> Create(Span<T> span, int index) => new Ref<T>(span.Slice(index, 1));
#endif
}
