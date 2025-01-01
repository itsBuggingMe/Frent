using Frent.Components;
using System.Diagnostics;

namespace Frent;

/// <summary>
/// A wrapper ref struct over a reference to a <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">The type this <see cref="Ref{T}"/> wraps over</typeparam>
/// <param name="comp">The reference to wrap</param>
public ref struct Ref<T>(ref T comp)
{
    /// <summary>
    /// The wrapped reference to <typeparamref name="T"/>
    /// </summary>
    public ref T Component = ref comp;
    public static implicit operator T(Ref<T> @ref) => @ref.Component;
    public override readonly string ToString() => Component?.ToString() ?? "null";
}
