using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseBitsetTests
{
    [Test]
    public void SparseComponent_SurvivesMovesAndSlotReuse()
    {
        using World world = new();

        Entity a = world.Create(new SimpleSparseComponent(42), new Struct1(1));

        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));

        a.Add(new Struct2(2));
        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));

        a.Remove<Struct2>();
        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));

        a.Tag<Struct3>();
        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));

        a.Detach<Struct3>();
        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));

        Entity b = world.Create(new SimpleSparseComponent(7), new Struct1(11));
        Entity c = world.Create(new Struct1(12));

        b.Delete();

        Entity d = world.Create(new Struct1(13));

        That(d.Has<SimpleSparseComponent>(), Is.False);
        That(c.Has<SimpleSparseComponent>(), Is.False);
        That(c.Get<Struct1>().Value, Is.EqualTo(12));
        That(a.Has<SimpleSparseComponent>(), Is.True);
        That(a.Get<SimpleSparseComponent>().Value, Is.EqualTo(42));
        That(a.Get<Struct1>().Value, Is.EqualTo(1));

        List<Entity> withSparse = [];
        foreach (var (entity, _) in world.Query<SimpleSparseComponent>().EnumerateWithEntities<SimpleSparseComponent>())
            withSparse.Add(entity);
        That(withSparse, Is.EquivalentTo(new[] { a }));
    }
}
