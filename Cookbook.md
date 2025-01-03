# Frent Cookbook

## Component Composition
Frent allows you to create entities which are a composition of unqiue components. Components can contain behavior and data.
### Behavior using `IUpdateComponent`

You must implement one of many interfaces for component behavior, while any type can be a component. The first of these are `IUpdateComponent` and `IUpdateComponent<T1, T2, ...>` (up to `T15`).

`IUpdateComponent` represents a component that doesn't take any other components as an input; adding generic types adds component arguments to the `Update` function.

#### Example:

```csharp
using World world = new World();

//Create three entities
for(int i = 0; i < 3; i++)
{
    world.Create<string, ConsoleText>("\"Hello, World!\"", new()
    { 
        TextColor = ConsoleColor.Blue,
    });
}

//Update the three entities
world.Update();

struct ConsoleText(ConsoleColor Color) : IUpdateComponent<string>
{
    public void Update(ref string str)
    {
        Console.SetForegroundColor(Color);
        Console.Write(str);
    }
}
```
#### Output:
```csharp
"Hello World!""Hello World!""Hello World!"
```

### Entity and Uniforms

You can access **uniforms** (aka constant data, singleton components) and an entity's own `Entity` struct within a component's `Update` function. The interfaces are `IUniformUpdateComponent` and `IEntityUpdateComponent`, respectively. There is also an additional `IEntityUniformUpdateComponent` interface. Each of these interfaces also have their own versions with generic arguments up to `T15`. Interfaces using uniforms also require a first generic argument specifying uniform type.

Uniforms are injected through an `IUniformProvider`

#### Example:

```csharp
IUniformProvider uniforms = new DictionaryUniformProvider();
//add delta time as a float
uniforms.Add<float>(0.5f);

using World world = new World();

world.Create<Velocity, Position>();
world.Create<Position>();

world.Update();

record struct Position(float X) : IEntityUpdateComponent
{
    public void Update(Entity entity)
    {
        Console.WriteLine(entity.Has<Velocity>() ? 
            "I have velocity!" : 
            "No velocity here!");
    }
}

record struct Velocity(float DX) : IUniformUpdateComponent<float, Position>
{
    public void Update(in float dt, ref Position pos)
    {
        pos.X += DX * dt;
    }
}
```
#### Output:
```
I have velocity!
No velocity here!
```
