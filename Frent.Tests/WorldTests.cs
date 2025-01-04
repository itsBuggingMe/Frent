using Frent.Core;
using System.Runtime.CompilerServices;
using static NUnit.Framework.Assert;
using static System.Linq.Enumerable;

namespace Frent.Tests;

public class WorldTests
{
    [Test]
    public void World_Create_ID()
    {
        for(int i = 0; i < 100; i++)
        {
            new World().Dispose();
        }

        using World world = new World();
        That(world.Version == (255 - 101), "101 worlds have been created");
        That(world.ID == 0, "ID should be recycled");
    }

    [Test]
    public void World_Uniform_Throws()
    {
        using World world = new World();
        world.Create<UniformComponent, int>(default, default);
        Throws<InvalidOperationException>(() => world.Update(), "World wasn't given a uniform provider");
    }

    [Test]
    public void World_Uniform()
    {
        using World world = new World();
        world.UniformProvider = new DefaultUniformProvider().Add(3);
        var ent = world.Create<UniformComponent, int>(default, default);
        world.Update();
        That(ent.Get<int>() == 3);
    }

    [Test]
    public void World_Create()
    {
        using World world = new World();
        Entity[] e = Range(0, 1000).Select(_ => world.Create<UpdateComponent1, int>(default, default)).ToArray();
        That(e.All(e => e.IsAlive()), "No entities have been killed");
        for(int i = 0; i < e.Length; i++)
            e[i].Delete();
        That(e.All(e => !e.IsAlive()), "All entities have been killed");
        var x = e.Where(e => e.IsAlive());
    }
    
    [Test]
    public void World_Query_Uniform()
    {
        var (w, q) = GenerateWorld<DummyComponent>();

        Throws<InvalidOperationException>(() => w.QueryUniform((in float uniform, ref int x) => { }), "No uniform provider provided");

        w.Dispose();
    }

    [Test]
    public void World_Query_UpdateComponent([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new CounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(CounterComponent)}");
    }

    [Test]
    public void World_Query_UpdateComponentWithArg([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new ArgCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(ArgCounterComponent)}");
    }

    [Test]
    public void World_Query_UniformUpdateComponent([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new UniformCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(UniformCounterComponent)}");
    }

    [Test]
    public void World_Query_UniformUpdateComponentWithArg([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new UniformArgCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(UniformArgCounterComponent)}");
    }

    [Test]
    public void World_Query_EntityUpdateComponent([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new EntityCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(EntityCounterComponent)}");
    }

    [Test]
    public void World_Query_EntityUpdateComponentWithArg([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new EntityArgCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(EntityArgCounterComponent)}");
    }

    [Test]
    public void World_Query_EntityUniformUpdateComponent([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new EntityUniformCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(EntityUniformCounterComponent)}");
    }

    [Test]
    public void World_Query_EntityUniformUpdateComponentWithArg([Values(0, 1, 2, 500, 1_000, 5_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunQueryTest(new EntityUniformArgCounterComponent(box), iterations), Is.EqualTo(box.Value),
            $"Component updates were skipped in {nameof(EntityUniformArgCounterComponent)}");
    }

    [Test]
    public void World_InlineQuery_CounterQuery([Values(0, 1, 2, 500, 1_000, 10_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunInlineQueryTest(w => w.InlineQuery<InlineCounterQuery, int>(new(box)), iterations), Is.EqualTo(box.Value),
            $"Inline query updates were skipped in {nameof(InlineCounterQuery)}");
    }

    [Test]
    public void World_InlineQuery_ArgCounterQuery([Values(0, 1, 2, 500, 1_000, 10_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunInlineQueryTest(w => w.InlineQuery<InlineArgCounterQuery, int, int>(new(box)), iterations), Is.EqualTo(box.Value),
            $"Inline query updates were skipped in {nameof(InlineArgCounterQuery)}");
    }

    [Test]
    public void World_InlineQuery_UniformCounterQuery([Values(0, 1, 2, 500, 1_000, 10_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunInlineQueryTest(w => w.InlineQueryUniform<InlineUniformCounterQuery, int, int>(new(box)), iterations), Is.EqualTo(box.Value),
            $"Inline query updates were skipped in {nameof(InlineUniformCounterQuery)}");
    }

    [Test]
    public void World_InlineQuery_UniformArgCounterQuery([Values(0, 1, 2, 500, 1_000, 10_000, 512, 2048)] int iterations)
    {
        var box = new StrongBox<int>(0);
        That(RunInlineQueryTest(w => w.InlineQueryUniform<InlineUniformArgCounterQuery, int, int, int>(new(box)), iterations), Is.EqualTo(box.Value),
            $"Inline query updates were skipped in {nameof(InlineUniformArgCounterQuery)}");
    }

    private int RunInlineQueryTest(Action<World> runQuery, int iterations)
    {
        var (world, query) = GenerateWorld(new object(), 0, iterations);

        int entityCount = query.Where(e => e.Has<int>()).Count();

        runQuery(world);

        world.Dispose();

        return entityCount;
    }

    private int RunQueryTest<T>(T component, int iterations) where T : struct
    {
        var (world, query) = GenerateWorld(component, 0, iterations);

        int entityCount = query.Where(e => e.Has<T>()).Count();

        world.Update();
        world.Dispose();

        return entityCount;
    }

    private (World, Queue<Entity>) GenerateWorld<T>(T value = default!, object? uniform = null, int iterations = 5_000)
    {
        DefaultUniformProvider defaultUniformProvider = new();
        if (uniform is not null)
            defaultUniformProvider.Add(uniform.GetType(), uniform);

        World world = new World(defaultUniformProvider);

        Queue<Entity> entities = new();

        for (int i = 0; i < iterations; i++)
        {
            Entity e = world.Create<int>(default);
            int mod = i % 3;

            if (mod == 0)
                e.Add<Component1>(default);
            if (mod == 1)
                e.Add<Component2>(default);
            if (mod == 2)
                e.Add<Component3>(default);
            entities.Enqueue(e);

            if (i % 5 == 0)
            {
                entities.Enqueue(world.Create<UpdateComponent1, int>(default, default));
            }
            if (i % 11 == 0)
            {
                entities.Enqueue(world.Create<UpdateComponent2, long, int>(default, default, default));
            }

            if (i % 17 == 0)
            {
                Entity special;
                entities.Enqueue(special = world.Create(value));
                special.Add(0);

                if (i % 2 == 0)
                    special.Add<Component1>(default);
                if (i % 3 == 0)
                    special.Add<Component2>(default);
                if (i % 4 == 0)
                    special.Add<Component3>(default);
            }


            if (i % 23 == 0)
            {
                entities.Dequeue().Delete();
            }

            if(entities.Any(e => !e.Has<int>()))
            {

            }
        }

        return (world, entities);
    }
}