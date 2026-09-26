# Links (relationships)

Links let you build a directed graph where you connect entities with links.

<div style="margin: 2rem; display: flex; justify-content: center;">
    <img src="../images/directed-graph.png" style="width:300px;" />
</div>

For example, the following code could be used to build the graph above:

```cs
World world = new();
Entity a = world.Create();

Entity b = world.Create();
Entity c = world.Create();

Entity d = world.Create();
Entity e = world.Create();

b.Link<ChildOf>(a);
c.Link<ChildOf>(a);

d.Link<ChildOf>(c);
e.Link<ChildOf>(c);

foreach (Entity child in a.EnumerateIncomingWithEntities<ChildOf>())
{
    // gets you b, c
}

struct ChildOf;
```

Entities can be linked to any other entity with any type, including linked with itself.