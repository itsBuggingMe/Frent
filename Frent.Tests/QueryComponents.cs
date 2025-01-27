using Frent.Components;
using Frent.Systems;
using System.Runtime.CompilerServices;

namespace Frent.Tests;

struct CounterComponent : IComponent
{
    public StrongBox<int> Counter;

    public CounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update() => Counter.Value++;
}

struct ArgCounterComponent : IComponent<int>
{
    public StrongBox<int> Counter;

    public ArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(ref int arg) => Counter.Value++;
}

struct UniformCounterComponent : IUniformComponent<int>
{
    public StrongBox<int> Counter;

    public UniformCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(int uniform) => Counter.Value++;
}

struct UniformArgCounterComponent : IUniformComponent<int, int>
{
    public StrongBox<int> Counter;

    public UniformArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(int uniform, ref int arg) => Counter.Value++;
}

struct EntityCounterComponent : IEntityComponent
{
    public StrongBox<int> Counter;

    public EntityCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity) => Counter.Value++;
}

struct EntityArgCounterComponent : IEntityComponent<int>
{
    public StrongBox<int> Counter;

    public EntityArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, ref int arg) => Counter.Value++;
}

struct EntityUniformCounterComponent : IEntityUniformComponent<int>
{
    public StrongBox<int> Counter;

    public EntityUniformCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, int uniform)
    {
        Counter.Value++;
    }
}

struct EntityUniformArgCounterComponent : IEntityUniformComponent<int, int>
{
    public StrongBox<int> Counter;

    public EntityUniformArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, int uniform, ref int arg) => Counter.Value++;
}

struct InlineCounterQuery : IAction<int>
{
    public StrongBox<int> Counter;

    public InlineCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(ref int component) => Counter.Value++;
}

struct InlineArgCounterQuery : IAction<int, int>
{
    public StrongBox<int> Counter;

    public InlineArgCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(ref int component, ref int arg) => Counter.Value++;
}

struct InlineUniformCounterQuery : IUniformAction<int, int>
{
    public StrongBox<int> Counter;

    public InlineUniformCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(int uniform, ref int component) => Counter.Value++;
}

struct InlineUniformArgCounterQuery : IUniformAction<int, int, int>
{
    public StrongBox<int> Counter;

    public InlineUniformArgCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(int uniform, ref int component, ref int arg) => Counter.Value++;
}