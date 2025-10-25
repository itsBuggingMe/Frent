using Frent.Components;

namespace Frent.Tests.SparseComponents;

internal struct SparseComponent(Action? OnUpdate, object Data) : ISparseComponent, IUpdate
{
    public object Data { get; } = Data;

    public void Update()
    {
        OnUpdate?.Invoke();
    }
}

internal struct SimpleSparseComponent : ISparseComponent
{
    public int Value;
    
    public SimpleSparseComponent(int value)
    {
        Value = value;
    }
}

internal struct SparseComponentWithEvents : ISparseComponent, IInitable, IDestroyable
{
    public bool InitCalled;
    public bool DestroyCalled;
    public Entity InitEntity;
    
    public void Init(Entity self)
    {
        InitCalled = true;
        InitEntity = self;
    }
    
    public void Destroy()
    {
        DestroyCalled = true;
    }
}

internal class SparseReferenceComponent : ISparseComponent, IInitable, IDestroyable
{
    public bool InitCalled { get; set; }
    public bool DestroyCalled { get; set; }
    public Entity InitEntity { get; set; }
    public string Data { get; set; } = string.Empty;
    
    public void Init(Entity self)
    {
        InitCalled = true;
        InitEntity = self;
    }
    
    public void Destroy()
    {
        DestroyCalled = true;
    }
}