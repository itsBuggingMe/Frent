# Filtering Updates

By default, `World.Update()` calls every `Update` method on every component, in no guaranteed order. Update filters let you split your code into named "phases", such as `Tick` and `Render`, and then run each one of these phases separately.

### Defining and using a filter

A filter is an attribute class that inherits from `UpdateTypeAttribute`. You can apply it to any `Update` method, then call `World.Update<T>()` with your filter type to run only the methods marked with it.

```csharp
// Can also be called XYZAttribute since C# omits the suffix
class Tick : UpdateTypeAttribute;
class Render : UpdateTypeAttribute;

struct Player : IUpdate, IEntityUpdate
{
    [Tick]
    public void Update() => Console.WriteLine("Player Tick!");

    [Render]
    public void Update(Entity _) => Console.WriteLine("Player Render!");
}
```

> [!WARNING]
> `World.Update()` ignores filters and always runs every `Update` method, including filtered ones. If you call `World.Update<Tick>()` and then `world.Update()`, the `Tick` methods run twice. To avoid this, see [Exclusive Updates](#exclusive-updates).

### Updating a single component

For finer control, `world.UpdateComponent` runs all the `Update` methods of one component type, regardless of filters.

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0Aworld.Create%28new%20Player%28%29%29%3B%0A%0AConsole.WriteLine%28%22Calling%20Tick...%22%29%3B%0Aworld.Update%3CTick%3E%28%29%3B%0A%0AConsole.WriteLine%28%22Calling%20Render...%22%29%3B%0Aworld.Update%3CRender%3E%28%29%3B%0A%0AConsole.WriteLine%28%22Calling%20Player...%22%29%3B%0Aworld.UpdateComponent%28Component%3CPlayer%3E.ID%29%3B%0A%0Astruct%20Player%20%3A%20IUpdate%2C%20IEntityUpdate%0A%7B%0A%20%20%20%20%5BTick%5D%0A%20%20%20%20public%20void%20Update%28%29%20%3D%3E%20Console.WriteLine%28%22Player%20Tick%21%22%29%3B%0A%20%20%20%20%5BRender%5D%0A%20%20%20%20public%20void%20Update%28Entity%20_%29%20%3D%3E%20Console.WriteLine%28%22Player%20Render%21%22%29%3B%0A%7D%0A%0Aclass%20Tick%20%3A%20UpdateTypeAttribute%3B%0Aclass%20Render%20%3A%20UpdateTypeAttribute%3B&spans=5%7Ckeyword%7C1%7Cwhitespace%7C5%7Cclass-name%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Cwhitespace%7C1%7Coperator%7C1%7Cwhitespace%7C3%7Ckeyword%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C3%7Ckeyword%7C1%7Cwhitespace%7C6%7Cstruct-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C17%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C4%7Cclass-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C19%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C6%7Cclass-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C19%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Coperator%7C15%7Cmethod-name%7C1%7Cpunctuation%7C9%7Cclass-name%7C1%7Cpunctuation%7C6%7Cstruct-name%7C1%7Cpunctuation%7C1%7Coperator%7C2%7Cproperty-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C6%7Cstruct-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C1%7Cwhitespace%7C7%7Cinterface-name%7C1%7Cpunctuation%7C1%7Cwhitespace%7C13%7Cinterface-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C4%7Cclass-name%7C1%7Cpunctuation%7C5%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C4%7Ckeyword%7C1%7Cwhitespace%7C6%7Cmethod-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C2%7Coperator%7C1%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C14%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C6%7Cclass-name%7C1%7Cpunctuation%7C5%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C4%7Ckeyword%7C1%7Cwhitespace%7C6%7Cmethod-name%7C1%7Cpunctuation%7C6%7Cstruct-name%7C1%7Cwhitespace%7C1%7Cparameter-name%7C1%7Cpunctuation%7C1%7Cwhitespace%7C2%7Coperator%7C1%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C16%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cwhitespace%7C1%7Cpunctuation%7C2%7Cwhitespace%7C5%7Ckeyword%7C1%7Cwhitespace%7C4%7Cclass-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C1%7Cwhitespace%7C19%7Cclass-name%7C1%7Cpunctuation%7C1%7Cwhitespace%7C5%7Ckeyword%7C1%7Cwhitespace%7C6%7Cclass-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C1%7Cwhitespace%7C19%7Cclass-name%7C1%7Cpunctuation&output=Calling%20Tick...%0APlayer%20Tick%21%0ACalling%20Render...%0APlayer%20Render%21%0ACalling%20Player...%0APlayer%20Tick%21%0APlayer%20Render%21%0A" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>

### Exclusive updates

Pass `exclusiveUpdate: true` to `world.Update` to run only the methods that have NO filter attribute. Filtered methods are skipped, so you can run your filters first and then run everything else without any method being called twice.

```csharp
class Physics : UpdateTypeAttribute;

struct Player : IUpdate, IEntityUpdate
{
    [Physics]
    public void Update() => Console.WriteLine("Player Physics!");

    public void Update(Entity _) => Console.WriteLine("Player Always!");
}

using World world = new();
world.Create(new Player());

world.Update<Physics>();              // Player Physics!
world.Update(exclusiveUpdate: true);  // Player Always!
```

Replacing the last call with `world.Update()` would also print `Player Physics!` a second time.

### Choosing an Update Method

Here is a brief summary to help you pick which update method is the correct one for you:

| Call                                  | Runs                                            |
|---------------------------------------|-------------------------------------------------|
| `world.Update()`                      | Every `Update` method, filtered or not          |
| `world.Update<T>()`                   | Only methods marked with filter `T`             |
| `world.Update(exclusiveUpdate: true)` | Only methods with no filter attribute           |
| `world.UpdateComponent(id)`           | All `Update` methods of a single component type |