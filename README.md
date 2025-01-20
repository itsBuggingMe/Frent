# [Frent](https://itsbuggingme.github.io/Frent/) 
[![NuGet](https://img.shields.io/nuget/v/Frent.svg)](https://www.nuget.org/packages/Frent/) [![NuGet](https://img.shields.io/nuget/dt/Frent.svg)](https://www.nuget.org/packages/Frent/) ![GitHub commit activity (branch)](https://img.shields.io/github/commit-activity/w/itsBuggingMe/Frent/master)

A high preformance archetyped based **[ECF](https://itsbuggingme.github.io/Frent/docs/ecf.html)/[ECS](https://github.com/SanderMertens/ecs-faq)**  library for C#.

*Whaaaat?! Isn't there enough ECS libraries out there!*

While Frent's implementation is an archetype based ECS, thats not why Frent was made. Frent is primarily an **ECF** - Entity Component Framework - that allows you to easily use composition for code reuse rather than inheritance with minimal boilerplate. Think Unity's Monobehavior powered by the principles and speed of an ECS, as well as less boilerplate.

> [!CAUTION]
> Frent is still in beta and is not completely stavle.

## Quick Example

```csharp
using Frent;
using Frent.Components;
using System.Numerics;

using World world = new World();
Entity entity = world.Create<Position, Velocity>(new(Vector2.Zero), new(Vector2.One));

//Call Update to run the update functions of your components
world.Update();

// Position is <2,2>
Console.WriteLine(entity.Get<Position>().Value);

record struct Position(Vector2 Value);
record struct Velocity(Vector2 Delta) : IComponent<Position>
{
    public void Update(ref Position position) => position.Value += Delta;
}
```

Wanna learn more? Check out the [docs](https://itsbuggingme.github.io/Frent/docs/getting-started.html)!

# Features
## Implemented
- [x]  Entity struct the size of a 64 bits
- [x]  Up to 16 components per entity
- [x]  Getting, Adding, and Removing components
- [x]  Classes as components
- [x]  Structs as components
- [x]  Deconstructing entities
- [x]  Component memory stored contiguously (when using structs)
- [x]  All entity functions are O(1) and highly optimised
- [x]  Pass in uniform data e.g., `deltaTime`
- [x]  Deconstructing entities
- [x]  Zero reflection
- [x]  AOT Compatible
- [x]  Built in Uniform Provider implementation
- [x]  Non-Generic Entity Creation
- [X]  Entity Tags
- [X]  World Update Filtering
- [X]  Multithreading

## Future
- [ ]  Comprehensive docs
- [ ]  100% Test coverage
- [ ]  More samples, examples, & explanations!
