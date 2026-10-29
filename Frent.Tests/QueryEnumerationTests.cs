using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class QueryEnumerationTests
{
    [Test]
    public static void Enumerate_SingleComponent_WritesThroughRef()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        foreach (ref var component in world.Query<Struct1>().Enumerate<Struct1>())
            component.Value += 10;

        That(a.Get<Struct1>().Value, Is.EqualTo(11));
        That(b.Get<Struct1>().Value, Is.EqualTo(12));
    }

    [Test]
    public static void TypedQuery_SingleComponent_WritesThroughRef()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));

        foreach (ref var component in world.Query<Struct1>())
            component.Value = 7;

        That(a.Get<Struct1>().Value, Is.EqualTo(7));
    }

    [Test]
    public static void TypedQuery_TwoComponents_YieldsTuple()
    {
        using World world = new();
        world.Create(new Struct1(3), new Struct2(4));

        List<(int, int)> found = [];
        foreach (var (first, second) in world.Query<Struct1, Struct2>())
            found.Add((first.Value.Value, second.Value.Value));

        That(found, Is.EqualTo(new[] { (3, 4) }));
    }
}
