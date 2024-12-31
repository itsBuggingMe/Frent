using Frent.Components;
using System.Diagnostics;

namespace Frent;

public ref struct Ref<T>(ref T comp)
{
    public ref T Component = ref comp;
    public static implicit operator T(Ref<T> @ref) => @ref.Component;
    public override string ToString() => Component?.ToString() ?? "null";
}
