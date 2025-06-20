# [Frent](https://itsbuggingme.github.io/Frent/) 
[![NuGet](https://img.shields.io/nuget/v/Frent.svg)](https://www.nuget.org/packages/Frent/) [![NuGet](https://img.shields.io/nuget/dt/Frent.svg)](https://www.nuget.org/packages/Frent/) ![GitHub commit activity (branch)](https://img.shields.io/github/commit-activity/m/itsBuggingMe/Frent/master) [![Help](https://img.shields.io/discord/1341196126291759188?label=help&color=5865F2&logo=discord)](https://discord.gg/TPWQQEvtg4) ![GitHub Repo stars](https://img.shields.io/github/stars/ItsBuggingMe/Frent)


A high performance, low memory usage, archetyped based **[EC](https://itsbuggingme.github.io/Frent/docs/ecf.html) framework/[ECS](https://github.com/SanderMertens/ecs-faq)**  library for C#.

*Whaaaat?! Aren't there enough ECS libraries out there!*

While Frent's implementation is an archetype based ECS, thats not why Frent was made. Frent is primarily an **EC** framework - Entity Component framework - that allows you to easily use composition for code reuse rather than inheritance with minimal boilerplate. Write components that include behavior, lifetime management, and events while enjoying all the performance benefits of an ECS.

Want to write systems anyways? Frent also has a Systems API that allows you to query entities in the style of an ECS.

> [!CAUTION]
> Frent is still in beta and is not completely stable.

## Quick Example

```csharp
using Frent;
using Frent.Systems;
using Frent.Components;
using System.Numerics;

using World world = new World();
Entity entity = world.Create<Position, Velocity>(new(Vector2.Zero), new(Vector2.One));

//Call Update to run the update functions of your components
world.Update();

// Position is (1, 1)
Console.WriteLine(entity.Get<Position>());

// Alternatively, use a system
world.Query<Position, Velocity>()
    .Delegate((ref Position p, ref Velocity v) => p.Value += v.Delta);

record struct Position(Vector2 Value);
record struct Velocity(Vector2 Delta) : IInitable, IComponent<Position>
{
    // There is also IDestroyable
    public void Init(Entity self) { }
    public void Update(ref Position position) => position.Value += Delta;
}
```

Wanna learn more? Check out the [docs](https://itsbuggingme.github.io/Frent/docs/getting-started.html)!

There is also samples for [Monogame](https://github.com/itsBuggingMe/Frent/blob/master/Frent.Sample/Asteroids/AsteroidsGame.cs), [Unity](https://github.com/itsBuggingMe/Frent.Unity.Sample) and [Godot](https://github.com/itsBuggingMe/FrentGodotSample).

## Performance

Frent is a lot faster than most C# ECS implementations - [Benchmark](https://github.com/Doraku/Ecs.CSharp.Benchmark).


# Features
## Implemented
- [x]  Tiny entity struct the size of 8 bytes
- [x]  Up to 127 components per entity
- [x]  `struct` & `class` as components
- [x]  Pass in uniform data automatically e.g., `deltaTime`
- [x]  AOT Compatible & Zero reflection
- [x]  Non-Generic APIs
- [X]  Tags
- [X]  Command buffer
- [X]  Events on entity creation/deletion, component add/remove
- [X]  Automatic structual change management
- [X] `EntityMarshal` and `WorldMarshal` for even faster speeds!
- [x]  Bulk operations
- [X]  Multithreading

## Future
- [ ]  Comprehensive docs
- [ ]  100% Test coverage
- [ ]  More samples, examples, & explanations!

# Contributing
Wanna help?

Report bugs, suggest APIs, and give general [feedback](https://github.com/itsBuggingMe/Frent/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22open%20for%20feedback%22).
Just open an issue before starting a large feature.