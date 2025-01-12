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