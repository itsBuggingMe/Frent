using Frent.Components;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal partial class CreateEntitiesTests
{
    private partial struct A { public int V; }
    private partial struct B { public int V; }

    private partial struct BulkIniterComponent : IInitable
    {
        public Entity Self;
        public void Init(Entity self) => Self = self;
    }

    private partial struct BulkSparseComponent : ISparseComponent
    {
        public int V;
    }

    [Test]
    public void CreateEntities_CreatesAllEntities()
    {
        using World world = new();
        Entity[] entities = new Entity[128];

        world.CreateEntities<A>(entities, new A { V = 7 });

        foreach (var e in entities)
        {
            That(e.IsNull, Is.False);
            That(e.Get<A>().V, Is.EqualTo(7));
        }

        int total = 0;
        foreach (Entity e in world.Query<A>().EnumerateWithEntities())
            total++;
        That(total, Is.EqualTo(128));
    }

    [Test]
    public void CreateEntities_MultiComponent_WritesEachValue()
    {
        using World world = new();
        Entity[] entities = new Entity[64];

        world.CreateEntities<A, B>(entities, new A { V = 3 }, new B { V = -5 });

        foreach (var e in entities)
        {
            That(e.Get<A>().V, Is.EqualTo(3));
            That(e.Get<B>().V, Is.EqualTo(-5));
        }
    }

    [Test]
    public void CreateEntities_RecyclesDeletedIds()
    {
        using World world = new();
        Entity[] first = new Entity[32];
        for (int i = 0; i < first.Length; i++)
            first[i] = world.Create(new A { V = 1 });
        foreach (var e in first)
            e.Delete();

        Entity[] recycled = new Entity[32];
        world.CreateEntities<A>(recycled, new A { V = 9 });

        var firstIds = first.Select(e => e.EntityID).ToHashSet();
        foreach (var e in recycled)
        {
            That(e.IsNull, Is.False);
            That(e.Get<A>().V, Is.EqualTo(9));
            That(firstIds.Contains(e.EntityID), Is.True);
        }
    }

    [Test]
    public void CreateEntities_SparseComponent_SetsValueAndBits()
    {
        using World world = new();
        Entity[] entities = new Entity[32];

        world.CreateEntities<BulkSparseComponent>(entities, new BulkSparseComponent { V = 4 });

        foreach (var e in entities)
        {
            That(e.Has<BulkSparseComponent>(), Is.True);
            That(e.Get<BulkSparseComponent>().V, Is.EqualTo(4));
        }

        int matched = 0;
        foreach (var t in world.Query<BulkSparseComponent>().Enumerate<BulkSparseComponent>())
            matched += t.Item1.Value.V;

        That(matched, Is.EqualTo(4 * 32));
    }

    [Test]
    public void CreateEntities_InvokesIniters()
    {
        using World world = new();
        Entity[] entities = new Entity[16];

        world.CreateEntities<BulkIniterComponent>(entities, default);

        foreach (var e in entities)
            That(e.Get<BulkIniterComponent>().Self, Is.EqualTo(e));
    }

    [Test]
    public void CreateEntities_RaisesEntityCreated()
    {
        using World world = new();
        int seen = 0;
        world.EntityCreated += _ => seen++;

        Entity[] entities = new Entity[16];
        world.CreateEntities<A>(entities, default);

        That(seen, Is.EqualTo(16));
    }

    [Test]
    public void CreateEntities_DuringEnumeration_Defers()
    {
        using World world = new();
        world.Create(new A { V = 1 });
        Entity[] created = new Entity[8];

        foreach (var t in world.Query<A>().Enumerate<A>())
        {
            world.CreateEntities<A>(created, new A { V = t.Item1.Value.V + 10 });
            break;
        }

        foreach (var e in created)
            That(e.Get<A>().V, Is.EqualTo(11));
    }

    [Test]
    public void CreateEntities_EmptySpan_NoOp()
    {
        using World world = new();
        world.CreateEntities<A>(Span<Entity>.Empty);
        That(world.EntityCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateEntities_MixedSparseAndArchetypical()
    {
        using World world = new();
        Entity[] entities = new Entity[32];

        world.CreateEntities<A, BulkSparseComponent>(entities, new A { V = 2 }, new BulkSparseComponent { V = 8 });

        foreach (var e in entities)
        {
            That(e.Get<A>().V, Is.EqualTo(2));
            That(e.Get<BulkSparseComponent>().V, Is.EqualTo(8));
        }

        int matched = 0;
        foreach (var t in world.Query<BulkSparseComponent, A>().Enumerate<BulkSparseComponent, A>())
            matched += t.Item1.Value.V + t.Item2.Value.V;

        That(matched, Is.EqualTo(10 * 32));
    }
}
