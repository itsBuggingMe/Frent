# World

<br />

In Frent, every entity, component, query, archetype and other EC/ECS objects lives inside a `World` object. It is also responsible for creating, updating and destroying the entities you manage in your project. Therefore, it's a good idea to have one! 😁

Here, let's create one:

```csharp

class Program
{
    public static void Main()
    {
        World world = new();
    
        Console.WriteLine("We have a world! Yay!");
    }
}
```

Cool!

... now what?

### Creating and updating entities

The main purpose of having a world is to manage and compose [entities](entity.md) that can be used in a project. Therefore, a world is not of great use if we don't have an entity to play with, so let's start by creating one and giving it a few [components](component.md).

```csharp
using System.Numerics;

record struct Position(Vector2 Value);

record struct Velocity(Vector2 Value) : IUpdate<Position>
{
    public void Update(ref Position pos)
    {
        pos.Value += Value;
        Console.WriteLine($"Updated position to {{X: {Value.X}, Y: {Value.Y}}}");
    }
}

class Program
{
    public static void Main()
    {
        World world = new();
        Entity sillyGuy = world.Create<Position, Velocity>(new(), new(new(5, 5)));

        while (true)
        {
            world.Update();
            /*
                Expected output:
                > Updated position to {X: 5, Y: 5}
                > Updated position to {X: 10, Y: 10}
                > Updated position to {X: 15, Y: 15}
                > ...
            */

        }
    }
}
```

And that's it!

> [!TIP]
> Don't forget to call `Dispose()` on your world object when you're done using it.

