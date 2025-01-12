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
for (int i = 0; i < 3; i++)
{
    world.Create<string, ConsoleText>("\"Hello, World!\"", new(ConsoleColor.Blue));
}

//Update the three entities
world.Update();

struct ConsoleText(ConsoleColor Color) : IUpdateComponent<string>
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

### Entity and Uniforms

You can access **uniforms** (aka constant data, singleton components) and an entity's own `Entity` struct within a component's `Update` function. The interfaces are `IUniformUpdateComponent` and `IEntityUpdateComponent`, respectively. There is also an additional `IEntityUniformUpdateComponent` interface. Each of these interfaces also have their own versions with generic arguments up to `T15`. Interfaces using uniforms also require a first generic argument specifying uniform type.

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

### Systems

Frent also supports directly querying and updating entities. There are two main types of queries, inline queries and delegate queries. Delegate queries are concise. However, they are slightly slower as they cannot be inlined by the JIT compiler. Inline queries use structs that implement the `IQuery`, `IQueryEntity`, `IQueryUniform`, or `IQueryEntityUniform` interfaces. These interfaces also have versions with up to 16 generic component arguments. 

#### Example:

```csharp
DefaultUniformProvider provider = new DefaultUniformProvider();
provider.Add<byte>(5);
using World world = new World(provider);

for (int i = 0; i < 5; i++)
    world.Create<int>(i);

world.Query((ref int x) => Console.Write($"{x++}, "));
Console.WriteLine();

world.InlineQueryUniform<WriteQuery, byte, int>(default(WriteQuery));
```
#### Output:
```
3, 4, 0, 1, 2,
9, 10, 6, 7, 8,
```
*Note how the update order of entities is not always the same as the order of creation.*
*Component update order within an entity will always be the same (first-last), but which entities are updated first varies.*

### The Entity Struct

The `Entity` struct is a powerful struct for getting accessing data about an entity.

#### Example:

```csharp
using World world = new World();
Entity ent = world.Create<int, double, float>(69, 3.14, 2.71f);
//true
Console.WriteLine(ent.IsAlive());
//true
Console.WriteLine(ent.Has<int>());
//false
Console.WriteLine(ent.Has<bool>());
//You can also add and remove components
ent.Add<string>("I like Frent");

if (ent.TryGet<string>(out Ref<string> strRef))
{
    Console.WriteLine(strRef);
    //reassign the string value
    strRef.Component = "Do you like Frent?";
}

//If we didn't add a string earlier, this would throw instead
Console.WriteLine(ent.Get<string>());

//You can also deconstruct components from the entity to reassign many at once
ent.Deconstruct(out Ref<double> d, out Ref<int> i, out Ref<float> f, out Ref<string> str);
d.Component = 4;
str.Component = "Hello, World!";

//You can also deconstruct like this - you just can't assign the value of the struct
//This also won't work with the tuple deconstruction syntax unfortunately due to a bug w/ the C# compiler
ent.Deconstruct(out string str1);
Console.WriteLine(str1);
```

#### Output:
```
True
True
False
I like Frent
Do you like Frent?
Hello, World!
```
