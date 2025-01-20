# Filtering Components
You can filter a subset of components when updating by applying an attribute to your update functions.
```csharp
using World w = new World();
w.Create<DrawComponent>(default);

//DrawComponent.Update is called
w.Update();
//DrawComponent.Update is called
w.Update<DrawAttribute>();
//DrawComponent.Update is not called
w.Update<SomeOtherAttribute>();

//declare the attribute to use
class DrawAttribute : Frent.Updating.UpdateTypeAttribute;
class SomeOtherAttribute : Frent.Updating.UpdateTypeAttribute;

record struct DrawComponent : IComponent
{
    [Draw]
    public void Update()
    {
        Console.WriteLine("Draw called!");	    
    }
}
```
