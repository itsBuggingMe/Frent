using NUnit.Framework;
using Frent.Core;
using Frent.Systems;
using Frent.Serialization;
using Frent.Tests.Helpers;
using System.Text.Json;
using static NUnit.Framework.Assert;
using Frent.Components;
using System.Text.Json.Serialization;

namespace Frent.Tests;

internal class SerializationTests
{
    [Test]
    public void TestBasicSerialization()
    {
        using World world = new();
        var entity = world.Create<int>(42);
        
        string json = JsonWorldSerializer.Default.Serialize(world);
        using World deserialized = JsonWorldSerializer.Default.Deserialize(json);
        
        var query = deserialized.CreateQuery().Build();
        Entity deserializedEntity = GetFirstEntity(query);
        That(deserializedEntity.Get<int>(), Is.EqualTo(42));
    }

    [Test]
    public void TestSerializationWithTags()
    {
        using World world = new();
        var entity = world.Create<int>(42);
        entity.Tag<Tag>();

        string json = JsonWorldSerializer.Default.Serialize(world);
        using World deserialized = JsonWorldSerializer.Default.Deserialize(json);

        var query = deserialized.CreateQuery().Build();
        Entity deserializedEntity = GetFirstEntity(query);
        That(deserializedEntity.Tagged<Tag>(), Is.True);
        That(deserializedEntity.Get<int>(), Is.EqualTo(42));
    }

    [Test]
    public void TestCustomSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            WriteIndented = true,
        };

        var serializer = new JsonWorldSerializer(options);
        using World world = new();
        world.Create<int>(42);

        string json = serializer.Serialize(world);
        That(json, Does.Contain("\n"));
        That(json, Does.Contain("42"));
    }

    [Test]
    public void TestQueryFiltering()
    {
        using World world = new();
        world.Create(1);
        world.Create("test");
        
        var query = world.CreateQuery()
            .With<int>()
            .Build();

        string json = JsonWorldSerializer.Default.Serialize(world, query);
        using World deserialized = JsonWorldSerializer.Default.Deserialize(json);

        var allQuery = deserialized.CreateQuery().Build();
        var intQuery = deserialized.CreateQuery().With<int>().Build();
        var stringQuery = deserialized.CreateQuery().With<string>().Build();

        That(CountEntities(allQuery), Is.EqualTo(1));
        That(CountEntities(intQuery), Is.EqualTo(1));
        That(CountEntities(stringQuery), Is.EqualTo(0));
    }

    [Test]
    public void TestStreamSerialization()
    {
        using World world = new();
        world.Create(42);

        using var stream = new MemoryStream();
        JsonWorldSerializer.Default.Serialize(stream, world);

        stream.Position = 0;
        using World deserialized = JsonWorldSerializer.Default.Deserialize(stream);

        var query = deserialized.CreateQuery().Build();
        Entity deserializedEntity = GetFirstEntity(query);
        That(deserializedEntity.Get<int>(), Is.EqualTo(42));
    }

    [Test]
    public void TestMultipleComponents()
    {
        using World world = new();
        world.Create(42, "test");

        string json = JsonWorldSerializer.Default.Serialize(world);
        using World deserialized = JsonWorldSerializer.Default.Deserialize(json);

        var query = deserialized.CreateQuery().Build();
        Entity deserializedEntity = GetFirstEntity(query);
        That(deserializedEntity.Get<int>(), Is.EqualTo(42));
        That(deserializedEntity.Get<string>(), Is.EqualTo("test"));
    }

    [Test]
    public void TestCustomConverter()
    {
        using World world = new();
        var archetype = EntityType.EntityTypeOf([Component<int>.ID], [Tag<Tag>.ID]);
        var compType = Component<int>.ID;

        world.Create(new CallbackTestComponent
        {
            ComponentType = compType,
            Archetype = archetype,
        });

        string json = JsonWorldSerializer.Default.Serialize(world);
        using World deserialized = JsonWorldSerializer.Default.Deserialize(json);

        var query = deserialized.CreateQuery().Build();
        Entity deserializedEntity = GetFirstEntity(query);
        That(deserializedEntity.Get<CallbackTestComponent>().ComponentType, Is.EqualTo(compType));
        That(deserializedEntity.Get<CallbackTestComponent>().Archetype, Is.EqualTo(archetype));
    }

    private static Entity GetFirstEntity(Query query)
    {
        foreach (var entity in query.EnumerateWithEntities())
        {
            return entity;
        }
        throw new InvalidOperationException("No entities found in query");
    }

    private static int CountEntities(Query query)
    {
        int count = 0;
        foreach (var _ in query.EnumerateWithEntities())
        {
            count++;
        }
        return count;
    }

    internal struct Tag : ITag;

    internal struct CallbackTestComponent
    {
        public EntityType Archetype { get; set; }
        public ComponentID ComponentType { get; set; }
    }
}