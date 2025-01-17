# Writing Components
Components in Frent can very quickly access other components on the same entity when updating and can be any `T` . However, it is recommended you use structs for performance reasons. 

Here is an example component that has no behavior and one field.
```csharp
record struct Component1(float X);
```
To add behavior, implement the `IComponent` interface. This update function will be called whenever `World.Update` is called.
```csharp
record struct Component2(float X) : IComponent
{
    public void Update()
	{
		X++;
	}
}
```
If you want to take other components on the same entity as input, add generic types to the interface - e.g. `IComponent<Component1>`. You can add up to 15 generic types, since 16 is the maximum number of components on an entity.
```csharp
record struct Component3(float X): IComponent<Component1>
{
    public void Update(ref Component1 comp)
    {
	    X += comp.X;
    }
}
```
You can also access the `Entity` struct itself by implementing an `IEntityComponent` interface.
```csharp
record struct Component4(float X) : IEntityComponent
{
	public void Update(Entity self)
	{
		if(self.TryGet<Component3>(out Ref<Component3> val))
			X += val.Ref.X;
	}
}
```
Uniforms can be accessed by implementing the `IUniformComponent<TUniform>` interface. The uniform type is specified by the first generic argument, and it is retrieved by calling `IUniformProvider.GetUniform<TUniform>`. Ensure you have set `World.UniformProvider` before using uniforms
```csharp
record struct Component5(float X) : IUniformComponent<float>
{
    public void Update(in float uniform)
    {
        X += uniform;
    }
}
```
In addition there is an `IEntityUniformComponent` interface for when you need to use both uniforms and entities in an update function. All of these interfaces have their own additional generic interfaces that allow up for 15 component arguments.

### Interface Summary

`IComponent`

`IComponent<T1, ...>`


`IEntityComponent`

`IEntityComponent<T1, ...>`


`IUniformComponent<TUniform>`

`IUniformComponent<TUniform, T1, ...>`


`IEntityUniformComponent<TUniform>`

`IEntityUniformComponent<TUniform, T1, ...>`