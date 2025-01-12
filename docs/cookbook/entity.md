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
