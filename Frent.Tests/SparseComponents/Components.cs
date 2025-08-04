using Frent.Components;

namespace Frent.Tests.SparseComponents;

internal struct SparseComponent(Action? OnUpdate, object Data) //: ISparseComponent, IComponent
{
    public object Data { get; } = Data;

    public void Update()
    {
        OnUpdate?.Invoke();
    }
}