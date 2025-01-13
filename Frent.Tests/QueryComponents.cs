using System.Diagnostics;
using System.Runtime.CompilerServices;
using Frent.Components;
using Frent.Systems;

namespace Frent.Tests;

struct CounterComponent : IUpdateComponent
{
    public StrongBox<int> Counter;

    public CounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update() => Counter.Value++;
}

struct ArgCounterComponent : IUpdateComponent<int>
{
    public StrongBox<int> Counter;

    public ArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(ref int arg) => Counter.Value++;
}

struct UniformCounterComponent : IUniformUpdateComponent<int>
{
    public StrongBox<int> Counter;

    public UniformCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(in int uniform) => Counter.Value++;
}

struct UniformArgCounterComponent : IUniformUpdateComponent<int, int>
{
    public StrongBox<int> Counter;

    public UniformArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(in int uniform, ref int arg) => Counter.Value++;
}

struct EntityCounterComponent : IEntityUpdateComponent
{
    public StrongBox<int> Counter;

    public EntityCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity) => Counter.Value++;
}

struct EntityArgCounterComponent : IEntityUpdateComponent<int>
{
    public StrongBox<int> Counter;

    public EntityArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, ref int arg) => Counter.Value++;
}

struct EntityUniformCounterComponent : IEntityUniformUpdateComponent<int>
{
    public StrongBox<int> Counter;

    public EntityUniformCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, in int uniform)
    {
        if(Counter is null)
        { 
        }
        Counter.Value++;
    }
}

struct EntityUniformArgCounterComponent : IEntityUniformUpdateComponent<int, int>
{
    public StrongBox<int> Counter;

    public EntityUniformArgCounterComponent(StrongBox<int> counter) => Counter = counter;

    public void Update(Entity entity, in int uniform, ref int arg) => Counter.Value++;
}

struct InlineCounterQuery : IQuery<int>
{
    public StrongBox<int> Counter;

    public InlineCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(ref int component) => Counter.Value++;
}

struct InlineArgCounterQuery : IQuery<int, int>
{
    public StrongBox<int> Counter;

    public InlineArgCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(ref int component, ref int arg) => Counter.Value++;
}

struct InlineUniformCounterQuery : IQueryUniform<int, int>
{
    public StrongBox<int> Counter;

    public InlineUniformCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(in int uniform, ref int component) => Counter.Value++;
}

struct InlineUniformArgCounterQuery : IQueryUniform<int, int, int>
{
    public StrongBox<int> Counter;

    public InlineUniformArgCounterQuery(StrongBox<int> counter) => Counter = counter;

    public void Run(in int uniform, ref int component, ref int arg) => Counter.Value++;
}