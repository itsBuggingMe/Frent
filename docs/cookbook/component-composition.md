## Component Composition
Frent allows you to create entities which are a composition of unqiue components. Components can contain behavior and data.
### Behavior using `IUpdateComponent`

You must implement one of many interfaces for component behavior, while any type can be a component. The first of these are `IUpdateComponent` and `IUpdateComponent<T1, T2, ...>` (up to `T15`).

`IUpdateComponent` represents a component that doesn't take any other components as an input; adding generic types adds component arguments to the `Update` function.

#### Example:

```csharp
using World world = new World();

//Create three entities
for (int i = 0; i < 3; i++)
{
    world.Create<string, ConsoleText>("\"Hello, World!\"", new(ConsoleColor.Blue));
}

//Update the three entities
world.Update();

struct ConsoleText(ConsoleColor Color) : IComponent<string>
{
    public void Update(ref string str)
    {
        Console.ForegroundColor = Color;
        Console.Write(str);
    }
}
```
#### Output:
```csharp
"Hello World!""Hello World!""Hello World!"
```