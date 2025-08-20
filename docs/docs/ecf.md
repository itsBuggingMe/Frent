# Introduction to Entity Component Frameworks

What's an EC (Entity Component) framework?

> An EC framework is where you create *entities* which are made of *components* inside a *framework*.

That definition wasn't too useful. Lets zoom in.

### What is an entity?

Entities themselves do not do anything - you can think of them as just IDs. Instead, an entity has its own components which do the heavy lifting.

### What is a component?

Components can contain data and behavior about your game. You can have a component with only data or a component with only behavior. For example, a `Location` component might just contain data in the form of a `X` and `Y` coordinate, while a `Velocity` component might contain a `DX` and `DY` fields as well as behavior to change the `Location` coordinate.

```csharp
//only contains X and Y
record struct Position(float X, float Y);
record struct Velocity(float DX, float DY) : IComponent<Position>
{
    //has its own DX, DY, AND updates the position
    public void Update(ref Position position)
    {
        position.X += DX;
        position.Y += DY;
    }
}
```

> Syntax?
> *what is a [record struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/record-structs)?*
> *what is a [ref](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#reference-parameters)?*

### Why components?

You may have heard the phrase:

> "Composition over inheritance"

Components follow composition, allowing for extremely simple code reuse. A health component for example can be given to an enemy entity, or a player entity, or a rock. There is no need to plan and force behaviors into unmaintainable long complex chains of inheritance.

Other notable projects that use an EC framework are [Monocle](https://github.com/JamesMcMahon/monocle-engine) and Unity.

### The Framework:

Frent takes composition to the next level by removing or alleviating overheads normally associated with heavy composition (virtual calls, object overhead, garbage collection), replacing them with the speed advantages of an [ECS](https://github.com/SanderMertens/ecs-faq). With Frent, you get the best of all worlds. The __intuition__ of OOP programming, __flexibility__ of component oriented programming style, and the __performance__ of an ECS.

Don't like components with behavior? Since Frent is powered by an ECS internally, it also exposes a way to directly query entities in the style of a regular ECS, which is just as fast.

### The editor advantage

Frent is also easy to make an editor for. While generic overloads, `Add<T>`, `Remove<T>`, `Create<T>` do offer the best performance, there are an additional APIs for adding components when the type is not known at compile time. This makes it easy to make an editor using Frent, where components can be haphazardly placed together to create new entities - no code required!
