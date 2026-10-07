using Frent.Components;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal partial class BatchStructuralTests
{
    private partial struct A { public int V; }
    private partial struct B { public int V; }
    
    private partial struct BatchAdded { public int V; }
    private partial struct BatchIniterComponent : IInitable
    {
        public Entity Self;
        public void Init(Entity self) => Self = self;
    }

    private partial struct BatchSparseComponent : ISparseComponent
    {
        public int V;
    }

    [Test]
    public void AddComponent_MovesAllMatchingEntities()
    {
        using World world = new();
        Entity[] entities = new Entity[100];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create(new A { V = i }, new B { V = -i });
        }

        world.AddComponent<BatchAdded>(world.Query<A>());

        foreach (var e in entities)
        {
            That(e.Has<BatchAdded>(), Is.True);
            That(e.Get<B>().V, Is.EqualTo(-e.Get<A>().V));
        }
    }

    [Test]
    public void AddComponent_WithValue_SetsAll()
    {
        using World world = new();
        Entity[] entities = new Entity[100];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create(new A { V = i });
        }

        world.AddComponent(world.Query<A>(), new BatchAdded { V = 42 });

        foreach (var e in entities)
        {
            That(e.Get<BatchAdded>().V, Is.EqualTo(42));
        }
    }

    [Test]
    public void AddComponent_SkipsEntitiesThatAlreadyHaveComponent()
    {
        using World world = new();
        Entity with = world.Create(new A(), new BatchAdded { V = 7 });
        Entity without = world.Create(new A());

        world.AddComponent<BatchAdded>(world.Query<A>());

        That(with.Get<BatchAdded>().V, Is.EqualTo(7));
        That(without.Has<BatchAdded>(), Is.True);
    }

    [Test]
    public void RemoveComponent_RemovesFromAllMatching()
    {
        using World world = new();
        Entity[] entities = new Entity[50];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create(new A { V = i }, new BatchAdded());
        }

        world.RemoveComponent<BatchAdded>(world.Query<A>());

        foreach (var e in entities)
        {
            That(e.Has<BatchAdded>(), Is.False);
            That(e.Get<A>().V, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void AddComponent_InvokesIniterPerEntity()
    {
        using World world = new();
        Entity[] entities = new Entity[10];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create(new A());
        }

        world.AddComponent<BatchIniterComponent>(world.Query<A>());

        foreach (var e in entities)
        {
            That(e.Get<BatchIniterComponent>().Self, Is.EqualTo(e));
        }
    }

    [Test]
    public void AddComponent_InvokesEvents()
    {
        using World world = new();
        CallHelper calls = new();
        world.ComponentAdded += (e, id) => calls.Call();

        Entity a = world.Create(new A());
        Entity b = world.Create(new A());
        b.OnComponentAdded += (e, id) =>
        {
            That(id.Type, Is.EqualTo(typeof(BatchAdded)));
            calls.Call();
        };

        world.AddComponent<BatchAdded>(world.Query<A>());

        calls.AssertCalled(3);
    }

    [Test]
    public void RemoveComponent_InvokesEventsBeforeDataIsGone()
    {
        using World world = new();
        CallHelper calls = new();
        Entity a = world.Create(new A(), new BatchAdded { V = 5 });
        a.OnComponentRemoved += (e, id) =>
        {
            That(id.Type, Is.EqualTo(typeof(BatchAdded)));
            That(e.Get<BatchAdded>().V, Is.EqualTo(5));
            calls.Call();
        };

        world.RemoveComponent<BatchAdded>(world.Query<BatchAdded>());

        calls.AssertCalled();
        That(a.Has<BatchAdded>(), Is.False);
    }

    [Test]
    public void AddComponent_Sparse_WorksPerEntity()
    {
        using World world = new();
        Entity[] entities = new Entity[10];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create(new A());
        }

        world.AddComponent(world.Query<A>(), new BatchSparseComponent { V = 3 });

        foreach (var e in entities)
        {
            That(e.Has<BatchSparseComponent>(), Is.True);
            That(e.Get<BatchSparseComponent>().V, Is.EqualTo(3));
        }

        world.RemoveComponent<BatchSparseComponent>(world.Query<A>());

        foreach (var e in entities)
        {
            That(e.Has<BatchSparseComponent>(), Is.False);
        }
    }

    [Test]
    public void AddComponent_PreservesLinks()
    {
        using World world = new();
        Entity parent = world.Create(new A());
        Entity child = world.Create(new B());
        child.Link<LinkTests.ChildOf>(parent);

        world.AddComponent<BatchAdded>(world.Query<B>());

        That(child.Has<BatchAdded>(), Is.True);
        That(child.HasOutgoingLink<LinkTests.ChildOf>(), Is.True);
        That(parent.HasIncomingLink<LinkTests.ChildOf>(), Is.True);
    }

    [Test]
    public void AddComponent_Deferred_DuringEnumeration()
    {
        using World world = new();
        Entity e = world.Create(new A());

        foreach (var tup in world.Query<A>().EnumerateWithEntities())
        {
            world.AddComponent<BatchAdded>(world.Query<A>());
            That(e.Has<BatchAdded>(), Is.False);
        }

        That(e.Has<BatchAdded>(), Is.True);
    }
}
