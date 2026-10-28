using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class ChunkEnumerationTests
{
    [Test]
    public static void ChunkEntities_AreAlignedWithComponentSpans()
    {
        using World world = new();
        const int Count = 64;
        Entity[] handles = new Entity[Count];
        for (int i = 0; i < Count; i++)
            handles[i] = world.Create(new Struct1(i));

        int seen = 0;
        foreach (var chunk in world.Query<Struct1>().EnumerateChunks<Struct1>())
        {
            chunk.Deconstruct(out Span<Struct1> components);
            var entities = chunk.Entities;
            for (int i = 0; i < components.Length; i++)
            {
                That(entities[i], Is.EqualTo(handles[components[i].Value]));
                seen++;
            }
        }

        That(seen, Is.EqualTo(Count));
    }

    [Test]
    public static void ChunkEntities_AreAlignedWithTwoComponentSpans()
    {
        using World world = new();
        const int Count = 32;
        for (int i = 0; i < Count; i++)
            world.Create(new Struct1(i), new Struct2(i * 2));

        int seen = 0;
        foreach (var chunk in world.Query<Struct1, Struct2>().EnumerateChunks<Struct1, Struct2>())
        {
            chunk.Deconstruct(out Span<Struct1> components, out Span<Struct2> doubled);
            var entities = chunk.Entities;
            for (int i = 0; i < components.Length; i++)
            {
                That(doubled[i].Value, Is.EqualTo(components[i].Value * 2));
                That(entities[i].Get<Struct1>().Value, Is.EqualTo(components[i].Value));
                seen++;
            }
        }

        That(seen, Is.EqualTo(Count));
    }

    [Test]
    public static void ChunkEntities_CoverAllEntitiesAcrossArchetypes()
    {
        using World world = new();
        const int Count = 16;
        var expected = new HashSet<Entity>();
        for (int i = 0; i < Count; i++)
            expected.Add(i % 2 == 0 ? world.Create(new Struct1(i), new Struct2(i)) : world.Create(new Struct1(i)));

        var seen = new HashSet<Entity>();
        int chunks = 0;
        foreach (var chunk in world.Query<Struct1>().EnumerateChunks<Struct1>())
        {
            chunks++;
            foreach (var entity in chunk.Entities)
                That(seen.Add(entity), Is.True);
        }

        That(seen.Count, Is.EqualTo(expected.Count));
        That(chunks, Is.GreaterThan(1));
    }

    [Test]
    public static void ChunkEntities_IndexerMatchesEnumerationOrder()
    {
        using World world = new();
        const int Count = 32;
        for (int i = 0; i < Count; i++)
            world.Create(new Struct1(i));

        foreach (var chunk in world.Query<Struct1>().EnumerateChunks<Struct1>())
        {
            var entities = chunk.Entities;
            int index = 0;
            foreach (var entity in entities)
                That(entities[index++], Is.EqualTo(entity));
            That(index, Is.EqualTo(chunk.Span.Length));
        }
    }

    [Test]
    public static void ChunkEntities_EmptyQueryYieldsNoChunks()
    {
        using World world = new();

        foreach (var _ in world.Query<Struct1>().EnumerateChunks<Struct1>())
            Fail("No chunks were expected");
    }
}
