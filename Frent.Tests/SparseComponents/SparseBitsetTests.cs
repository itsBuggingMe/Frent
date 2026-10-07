using Frent.Core;
using Frent.Core.Archetypes;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class SparseBitsetTests
{
    [Test]
    public void StructuralChanges_WithoutSparseComponents_DoNotAllocateBitsets()
    {
        using World world = new();

        Entity[] entities = new Entity[100];
        for (int i = 0; i < entities.Length; i++)
            entities[i] = world.Create(new Struct1(i));

        foreach (var e in entities)
            e.Add(new Struct2(1));

        foreach (var e in entities)
            e.Remove<Struct2>();

        foreach (var e in entities)
            e.Tag<Struct3>();

        foreach (var e in entities)
            e.Detach<Struct3>();

        for (int i = 0; i < entities.Length; i += 2)
            entities[i].Delete();

        foreach (var item in world.WorldArchetypeTable)
        {
            if (item.Archetype is { } archetype)
                That(archetype.BitsetArray.Length, Is.EqualTo(0));
            if (item.DeferredCreationArchetype is { } deferred)
                That(deferred.BitsetArray.Length, Is.EqualTo(0));
        }
    }

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
