using Frent.Components;

namespace Frent.Tests.Helpers;

internal class Class1;
internal class Class2;
internal class Class3;
internal record struct Struct1(int Value);
internal record struct Struct2(int Value);
internal record struct Struct3(int Value);

internal class DelegateBehavior(Action onUpdate) : IUpdate
{
    public void Update() => onUpdate();
}

internal class FilteredBehavior1(Action onUpdate, Action<Entity>? onInit = null, Action? onDestroy = null) : IUpdate, IInitable, IDestroyable
{
    public void Init(Entity self) => onInit?.Invoke(self);
    public void Destroy() => onDestroy?.Invoke();
    [FilterAttribute1]
    public void Update() => onUpdate();
}

internal class FilteredBehavior2(Action onUpdate) : IUpdate
{
    [FilterAttribute2]
    public void Update() => onUpdate();
}

internal struct DependencyComponent : IUpdate<int>
{
    [FilterAttribute1]
    public void Update(ref int arg)
    {

    }
}

internal class ChildClass : BaseClass;
internal class BaseClass;

partial struct GenericComponent<T>() : IUpdate
{
    public int CalledCount;
    public void Update() => CalledCount++;
}