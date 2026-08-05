# Uniforms

Uniforms are shared per-world data, which is useful for values such as delta time.
Uniforms are automatically injected into update methods that implement an `IUniformUpdate<T...>` or `IEntityUniformUpdate<T...>` interface.
To provide uniforms, set the `UniformProvider` on the `World` instance. `DefaultUniformProvider` is suitable for most applications.
The uniform provider behaves similarly to a service container.

```cs
DefaultUniformProvider u = new();
u.Add(1f);

World world = new(u);
```


### Multiple uniforms

If you want to inject multiple different uniforms, simply use a tuple of uniforms as your generic argument, e.g. `IUniformUpdate<(float DeltaTime, Graphics G)>`.