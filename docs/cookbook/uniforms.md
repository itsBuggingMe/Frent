### Uniforms

You can access **uniforms** (aka constant data, singleton components) and an entity's own `Entity` struct within a component's `Update` function. The interfaces are `IUniformComponent` and `IEntityComponent`, respectively. There is also an additional `IEntityUniformUpdateComponent` interface. Each of these interfaces also have their own versions with generic arguments up to `T15`. Interfaces using uniforms also require a first generic argument specifying uniform type.

Uniforms are injected through an `IUniformProvider`

#### Example:

```csharp
DefaultUniformProvider uniforms = new DefaultUniformProvider();
//add delta time as a float
uniforms.Add(0.5f);

using World world = new World();

world.Create<Velocity, Position>(default, default);
world.Create<Position>(default);

world.Update();

record struct Position(float X) : IEntityComponent
{
    public void Update(Entity entity)
    {
        Console.WriteLine(entity.Has<Velocity>() ?
            "I have velocity!" :
            "No velocity here!");
    }
}

record struct Velocity(float DX) : IUniformComponent<float, Position>
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
