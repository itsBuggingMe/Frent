### Systems

Frent also supports directly querying and updating entities. There are two main types of queries, inline queries and delegate queries. Delegate queries are concise. However, they are slightly slower as they cannot be inlined by the JIT compiler. Inline queries use structs that implement the `IAction` interfaces. These interfaces also have versions with up to 16 generic component arguments. 

#### Example:

```csharp
DefaultUniformProvider provider = new DefaultUniformProvider();
provider.Add<byte>(5);
using World world = new World(provider);

for (int i = 0; i < 5; i++)
    world.Create<int>(i);

world.Query<int>().Delegate((ref int x) => Console.Write($"{x++}, "));
Console.WriteLine();
        
world.Query<int>().Inline<WriteAction, int>(new WriteAction());

internal struct WriteAction : IAction<int>
{
    public void Run(ref int x) => Console.Write($"{x++}, ");
}
```
#### Output:
```
0, 1, 2, 3, 4,
1, 2, 3, 4, 5,
```
