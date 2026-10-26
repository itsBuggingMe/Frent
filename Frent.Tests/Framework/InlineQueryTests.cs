using Frent.Components;
using Frent.Systems;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;

internal class InlineQueryTests
{
    internal struct SparseValue(int value) : ISparseComponent
    {
        public int Value = value;
    }

    internal struct SparseMarker : ISparseComponent;

    private struct RecordAction(List<(Entity, int)> seen) : IEntityAction<Struct1>
    {
        public readonly void Run(Entity e, ref Struct1 arg) => seen.Add((e, arg.Value));
    }

    private struct IncrementAction : IEntityAction<Struct1>
    {
        public readonly void Run(Entity e, ref Struct1 arg) => arg.Value++;
    }

    private struct RecordThreeAction(List<(Entity, int)> seen) : IEntityAction<Struct1, Struct2, Struct3>
    {
        public readonly void Run(Entity e, ref Struct1 a, ref Struct2 b, ref Struct3 c) => seen.Add((e, a.Value + b.Value + c.Value));
    }

    private struct RecordSparseAction(List<(Entity, int)> seen) : IEntityAction<SparseValue>
    {
        public readonly void Run(Entity e, ref SparseValue arg) => seen.Add((e, arg.Value));
    }

    private struct RecordMixedAction(List<(Entity, int)> seen) : IEntityAction<Struct1, SparseValue>
    {
        public readonly void Run(Entity e, ref Struct1 a, ref SparseValue b) => seen.Add((e, a.Value * 100 + b.Value));
    }

    private static Dictionary<Entity, int> Expected(params (Entity, int)[] items) => items.ToDictionary(t => t.Item1, t => t.Item2);
    private static Dictionary<Entity, int> Seen(List<(Entity, int)> seen) => seen.ToDictionary(t => t.Item1, t => t.Item2);

    [Test]
    public void InlineWithEntities_PassesMatchingEntityAndComponent()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1>(new(2));
        var e3 = world.Create<Struct1, Struct2>(new(3), default);
        world.Create<Struct2>(new(4));

        List<(Entity, int)> seen = [];
        world.Query<Struct1>().InlineWithEntities<RecordAction, Struct1>(new(seen));

        That(seen, Has.Count.EqualTo(3));
        That(Seen(seen), Is.EquivalentTo(Expected((e1, 1), (e2, 2), (e3, 3))));
    }

    [Test]
    public void InlineWithEntities_WritesThroughRef()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1, Struct2>(new(5), default);

        world.Query<Struct1>().InlineWithEntities<IncrementAction, Struct1>(default);

        That(e1.Get<Struct1>().Value, Is.EqualTo(2));
        That(e2.Get<Struct1>().Value, Is.EqualTo(6));
    }

    [Test]
    public void InlineWithEntities_ManyEntities_StayAligned()
    {
        using World world = new();
        Dictionary<Entity, int> expected = [];
        for (int i = 0; i < 1000; i++)
        {
            Entity e = i % 2 == 0
                ? world.Create<Struct1>(new(i))
                : world.Create<Struct1, Struct2>(new(i), default);
            expected[e] = i;
        }

        List<(Entity, int)> seen = [];
        world.Query<Struct1>().InlineWithEntities<RecordAction, Struct1>(new(seen));

        That(seen, Has.Count.EqualTo(1000));
        That(Seen(seen), Is.EquivalentTo(expected));
    }

    [Test]
    public void InlineWithEntities_MultipleComponents()
    {
        using World world = new();
        var e1 = world.Create<Struct1, Struct2, Struct3>(new(1), new(10), new(100));
        var e2 = world.Create<Struct1, Struct2, Struct3, int>(new(2), new(20), new(200), 0);
        world.Create<Struct1, Struct2>(new(3), new(30));

        List<(Entity, int)> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .With<Struct2>()
            .With<Struct3>()
            .Build()
            .InlineWithEntities<RecordThreeAction, Struct1, Struct2, Struct3>(new(seen));

        That(Seen(seen), Is.EquivalentTo(Expected((e1, 111), (e2, 222))));
    }

    [Test]
    public void InlineWithEntities_EmptyQuery_DoesNothing()
    {
        using World world = new();
        world.Create<Struct2>(default);

        List<(Entity, int)> seen = [];
        world.Query<Struct1>().InlineWithEntities<RecordAction, Struct1>(new(seen));

        That(seen, Is.Empty);
    }

    [Test]
    public void InlineWithEntities_SparseComponent()
    {
        using World world = new();
        var e1 = world.Create<SparseValue>(new(1));
        var e2 = world.Create<Struct1>(default);
        e2.Add(new SparseValue(2));
        world.Create<Struct1>(default);
        var e3 = world.Create<SparseValue, Struct2>(new(3), default);

        List<(Entity, int)> seen = [];
        world.Query<SparseValue>().InlineWithEntities<RecordSparseAction, SparseValue>(new(seen));

        That(seen, Has.Count.EqualTo(3));
        That(Seen(seen), Is.EquivalentTo(Expected((e1, 1), (e2, 2), (e3, 3))));
    }

    [Test]
    public void InlineWithEntities_DenseAndSparseComponent()
    {
        using World world = new();
        var e1 = world.Create<Struct1, SparseValue>(new(1), new(5));
        world.Create<Struct1>(new(2));
        var e3 = world.Create<Struct1, Struct2>(new(3), default);
        e3.Add(new SparseValue(7));

        List<(Entity, int)> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .With<SparseValue>()
            .Build()
            .InlineWithEntities<RecordMixedAction, Struct1, SparseValue>(new(seen));

        That(Seen(seen), Is.EquivalentTo(Expected((e1, 105), (e3, 307))));
    }

    [Test]
    public void InlineWithEntities_SparseExclusion_SkipsEntities()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1>(new(2));
        e2.Add(new SparseMarker());
        var e3 = world.Create<Struct1>(new(3));

        List<(Entity, int)> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .Without<SparseMarker>()
            .Build()
            .InlineWithEntities<RecordAction, Struct1>(new(seen));

        That(Seen(seen), Is.EquivalentTo(Expected((e1, 1), (e3, 3))));
    }

    [Test]
    public void InlineWithEntities_SparseInclusion_FiltersDenseComponent()
    {
        using World world = new();
        world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1>(new(2));
        e2.Add(new SparseMarker());
        world.Create<Struct1>(new(3));

        List<(Entity, int)> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .With<SparseMarker>()
            .Build()
            .InlineWithEntities<RecordAction, Struct1>(new(seen));

        That(Seen(seen), Is.EquivalentTo(Expected((e2, 2))));
    }

    [Test]
    public void InlineWithEntities_DeleteDuringIteration_IsDeferred()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1>(new(2));

        int count = 0;
        world.Query<Struct1>().Delegate((Entity e, ref Struct1 s) =>
        {
            count++;
            e.Delete();
        });

        That(count, Is.EqualTo(2));
        That(e1.IsAlive, Is.False);
        That(e2.IsAlive, Is.False);
    }

    [Test]
    public void DelegateWithEntity_PassesMatchingEntityAndComponent()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1, Struct2>(new(2), default);

        List<(Entity, int)> seen = [];
        world.Query<Struct1>().Delegate((Entity e, ref Struct1 s) => seen.Add((e, s.Value)));

        That(Seen(seen), Is.EquivalentTo(Expected((e1, 1), (e2, 2))));
    }

    [Test]
    public void DelegateWithEntity_MultipleComponents_WritesThroughRef()
    {
        using World world = new();
        var e1 = world.Create<Struct1, Struct2>(new(1), new(10));
        var e2 = world.Create<Struct1, Struct2, Struct3>(new(2), new(20), default);

        List<Entity> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .With<Struct2>()
            .Build()
            .Delegate((Entity e, ref Struct1 a, ref Struct2 b) =>
            {
                seen.Add(e);
                a.Value += b.Value;
            });

        That(seen, Is.EquivalentTo(new[] { e1, e2 }));
        That(e1.Get<Struct1>().Value, Is.EqualTo(11));
        That(e2.Get<Struct1>().Value, Is.EqualTo(22));
    }

    [Test]
    public void DelegateWithEntity_SparseExclusion_SkipsEntities()
    {
        using World world = new();
        var e1 = world.Create<Struct1>(new(1));
        var e2 = world.Create<Struct1>(new(2));
        e2.Add(new SparseMarker());

        List<Entity> seen = [];
        world.CreateQuery()
            .With<Struct1>()
            .Without<SparseMarker>()
            .Build()
            .Delegate((Entity e, ref Struct1 _) => seen.Add(e));

        That(seen, Is.EqualTo(new[] { e1 }));
    }
}
