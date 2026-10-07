using Frent.Components;
using Frent.Core;
using Frent.Marshalling;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal partial class CloneTests
{
    internal partial struct CloneSparse(int value) : ISparseComponent
    {
        public int Value = value;
    }

    internal partial struct CloneInitable : IInitable
    {
        public Entity InitEntity;

        public void Init(Entity self) => InitEntity = self;
    }

    internal partial struct CloneSparseInitable(int value) : ISparseComponent, IInitable
    {
        public int Value = value;
        public Entity InitEntity;

        public void Init(Entity self) => InitEntity = self;
    }

    internal partial class CloneBehavior : IEntityUpdate
    {
        public Entity Result;

        public void Update(Entity self) => Result = self.Clone();
    }

    [Test]
    public void Clone_CopiesComponentsTagsAndSparse()
    {
        using World world = new();

        var e = world.Create(new Struct1(1), new CloneSparse(5));
        e.Tag<Struct2>();

        Entity clone = e.Clone();

        That(clone, Is.Not.EqualTo(e));
        That(clone.Get<Struct1>().Value, Is.EqualTo(1));
        That(clone.Get<CloneSparse>().Value, Is.EqualTo(5));
        That(clone.Tagged<Struct2>(), Is.True);

        clone.Get<Struct1>() = new Struct1(99);
        clone.Get<CloneSparse>() = new CloneSparse(7);

        That(e.Get<Struct1>().Value, Is.EqualTo(1));
        That(e.Get<CloneSparse>().Value, Is.EqualTo(5));
    }

    [Test]
    public void Clone_RunsInitersAndCreatedEvent()
    {
        using World world = new();

        var e = world.Create(new CloneInitable(), new CloneSparseInitable(1));

        int events = 0;
        Entity created = default;
        world.EntityCreated += entity =>
        {
            events++;
            created = entity;
        };

        Entity clone = e.Clone();

        That(events, Is.EqualTo(1));
        That(created, Is.EqualTo(clone));

        That(e.Get<CloneInitable>().InitEntity, Is.EqualTo(e));
        That(e.Get<CloneSparseInitable>().InitEntity, Is.EqualTo(e));

        That(clone.Get<CloneInitable>().InitEntity, Is.EqualTo(clone));
        That(clone.Get<CloneSparseInitable>().InitEntity, Is.EqualTo(clone));
    }

    [Test]
    public void Clone_DuringUpdate_IsDeferred()
    {
        using World world = new();

        var behavior = new CloneBehavior();
        var e = world.Create(behavior);

        world.Update();

        Entity clone = behavior.Result;

        That(clone.IsAlive, Is.True);
        That(clone, Is.Not.EqualTo(e));
        That(world.EntityCount, Is.EqualTo(2));
        That(clone.Get<CloneBehavior>(), Is.SameAs(behavior));
    }

    [Test]
    public void Clone_Deferred_ClearsStaleSparseBits()
    {
        using World world = new();

        var stale = world.Create(new Struct1(0));
        stale.Add(new CloneSparse(1));
        stale.Delete();

        var filler = world.Create(new Struct2(0));

        Entity source = default;
        Entity clone = default;
        foreach (var _ in world.CreateQuery().Build().EnumerateWithEntities())
        {
            source = world.Create(new Struct1(9));
            clone = source.Clone();
        }

        That(clone.IsAlive, Is.True);
        That(clone.Has<CloneSparse>(), Is.False);

        int sparseCount = 0;
        foreach (var entity in world.CreateQuery().With<CloneSparse>().Build().EnumerateWithEntities())
        {
            sparseCount++;
            That(entity, Is.Not.EqualTo(clone));
            That(entity, Is.Not.EqualTo(source));
        }

        That(sparseCount, Is.EqualTo(0));
        That(clone.Get<Struct1>().Value, Is.EqualTo(9));
    }

    [Test]
    public void MoveEntityAcrossWorlds_CopiesData()
    {
        using World sourceWorld = new();
        using World destinationWorld = new();

        var e = sourceWorld.Create(new Struct1(5), new CloneSparse(9));
        e.Tag<Struct2>();

        Entity moved = WorldMarshal.MoveEntityAcrossWorlds(destinationWorld, e, callIniters: true, callEvents: true);

        That(moved.IsAlive, Is.True);
        That(moved.World, Is.EqualTo(destinationWorld));
        That(moved.Get<Struct1>().Value, Is.EqualTo(5));
        That(moved.Get<CloneSparse>().Value, Is.EqualTo(9));
        That(moved.Tagged<Struct2>(), Is.True);

        That(e.IsAlive, Is.True);
    }

    [Test]
    public void MoveEntityAcrossWorlds_DestinationMidUpdate_Defers()
    {
        using World sourceWorld = new();
        using World destinationWorld = new();

        var e = sourceWorld.Create(new Struct1(3));

        Entity moved = default;
        destinationWorld.Create(new DelegateBehavior(() =>
            moved = WorldMarshal.MoveEntityAcrossWorlds(destinationWorld, e, true, true)));

        destinationWorld.Update();

        That(moved.IsAlive, Is.True);
        That(moved.World, Is.EqualTo(destinationWorld));
        That(moved.Get<Struct1>().Value, Is.EqualTo(3));
        That(e.IsAlive, Is.True);
    }
}
