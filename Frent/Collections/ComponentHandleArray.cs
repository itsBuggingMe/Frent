using Frent.Core;

namespace Frent.Collections;


internal ref struct ComponentHandleArray(Span<ComponentHandle> buffer)
{
    public Span<ComponentHandle> Span = buffer;
    public readonly int Length => Span.Length;
    public ref ComponentHandle this[int index] => ref Span[index];
    public static implicit operator ComponentHandleArray(Span<ComponentHandle> buffer) => new ComponentHandleArray(buffer);

    public void Dispose()
    {
        foreach (var componentHandle in Span)
        {
            componentHandle.Dispose();
        }
    }
}
