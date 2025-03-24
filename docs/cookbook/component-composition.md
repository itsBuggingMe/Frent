## Component Composition
Frent allows you to create entities which are a composition of unqiue components. Components can contain behavior and data.
### Behavior using `IComponent`

You must implement one of many interfaces for component behavior, while any type can be a component. The first of these are `IComponent` and `IComponent<T1, T2, ...>` (up to `T15`).

`IComponent` represents a component that doesn't take any other components as an input but has an update method to be called every `World.Update()` - adding generic types adds component arguments to the `Update` function.

#### Example:

```csharp
using World world = new World();

//Create three entities
for (int i = 0; i < 3; i++)
{
    world.Create<string, ConsoleTextWithColor>($"Hello, World! #{i + 1}", new(ConsoleColor.Blue));
}

//Update the three entities
world.Update();

struct ConsoleTextWithColor(ConsoleColor Color) : IComponent<string>
{
    //Get the string component of this entity.
    public void Update(ref string str)
    {
        Console.ForegroundColor = Color;
        Console.WriteLine(str);
    }
}
```

#### Output:
```csharp
Hello World #1
Hello World #2
Hello World #3
```

> [!WARNING]
> A component type should only implement one `Update` method. For example, a component that implements `IComponent<string>` should not implement `IEntityComponent<int>`