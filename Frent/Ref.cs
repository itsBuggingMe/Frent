using Frent.Components;

namespace Frent;

public ref struct Ref<T>(ref T comp)
    where T : IComponent
{
    public ref T Component = ref comp;
    public static implicit operator T(Ref<T> @ref) => @ref.Component;
}
