# Component Lifetimes

Components can optionally implement `IInitable` and `IDestroyable` for lifetime management. A component's lifetime begins when it is added to a world, and ends when it is removed for the world. For example, a component might begin its lifetime when added to an entity and end its lifetime when the parent entity is deleted.

The `IInitable` interface is used to define a component which has an `Init(Entity self)` function to be called whenever the component's lifetime begins.

The `IDestroyable` interface does the opposite, with the `Destroy()` method called on the end of the component's lifetime.

```csharp
using World world = new World();

//Init is called here
Entity entity = world.Create<Example>(default);

//Destroy is called here
entity.Delete();

internal struct Example : IInitable, IDestroyable
{
    private SomeDisposableResource _resource;
    public void Init(Entity self)
    {
        Console.WriteLine("init!");
        _resource = new();
    }

    public void Destroy()
    {
        Console.WriteLine("destroy!");
        _resource.Dispose();
    }
}
```

Output:
```
init!
destroy!
```

`Init` is analogous to Unity's `Start` method and `Destroy` is analogous to Unity's `OnDestroy` method.