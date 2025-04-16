# [Frent](https://itsbuggingme.github.io/Frent/)

[![NuGet](https://img.shields.io/nuget/v/Frent.svg)](https://www.nuget.org/packages/Frent/) [![NuGet](https://img.shields.io/nuget/dt/Frent.svg)](https://www.nuget.org/packages/Frent/) ![GitHub commit activity (branch)](https://img.shields.io/github/commit-activity/m/itsBuggingMe/Frent/master) [![Help](https://img.shields.io/discord/1341196126291759188?label=help&color=5865F2&logo=discord)](https://discord.gg/TPWQQEvtg4) ![GitHub Repo stars](https://img.shields.io/github/stars/ItsBuggingMe/Frent)

A high preformance, low memory usage, archetypical **[ECF](https://itsbuggingme.github.io/Frent/docs/ecf.html)/[ECS](https://github.com/SanderMertens/ecs-faq)**  library for C#.

*Whaaaat?! Aren't there enough ECS libraries out there!*

While Frent's implementation is an archetype based ECS, thats not why Frent was made. Frent is primarily an **ECF** - Entity Component Framework - that allows you to easily use composition for code reuse rather than inheritance with minimal boilerplate. Think Unity's Monobehavior powered by the principles and speed of an ECS, as well as less boilerplate.

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
world.Query<With<Position>, With<Velocity>>()
    .Delegate((ref Position p, ref Velocity v) => p.Value += v.Delta);

record struct Position(Vector2 Value);
record struct Velocity(Vector2 Delta) : IInitable, IComponent<Position>
{
    // Component lifetime management is first class with IInitable and IDestroyable
    public void Init(Entity self) { }
    public void Update(ref Position position) => position.Value += Delta;
}
```

Wanna learn more? Check out the [docs](https://itsbuggingme.github.io/Frent/docs/getting-started.html)!

There is also samples for [Monogame](https://github.com/itsBuggingMe/Frent/blob/master/Frent.Sample/Asteroids/AsteroidsGame.cs), [Unity](https://github.com/itsBuggingMe/Frent.Unity.Sample) and [Godot](https://github.com/itsBuggingMe/FrentGodotSample).

## Preformance

Frent is a lot faster than most C# ECS implementations - [Benchmark](https://github.com/Doraku/Ecs.CSharp.Benchmark).

# Features

## Implemented

- [X]  Tiny 64 bit `Entity` struct
- [X]  Up to 127 components per `Entity`
- [X]  Classes and structs as components
- [X]  Pass in uniform data e.g., `deltaTime`
- [X]  World Update Filtering
- [X]  Zero reflection
- [X]  AOT Compatible
- [X]  Non-Generic API
- [X]  Entity Tags
- [X]  Command buffer
- [X]  Automatic structual change management during updates

## Future

- [ ]  Comprehensive docs
- [ ]  100% Test coverage
- [ ]  More samples, examples, & explanations!
- [ ]  Multithreading

# Contributing

Wanna help?

Report bugs, suggest APIs, and give general feedback.
Just open an issue before starting a large feature.
