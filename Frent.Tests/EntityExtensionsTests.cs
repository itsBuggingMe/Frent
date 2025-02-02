using Frent.Tests.Helpers;
using Frent.Core;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class EntityExtensionsTests
{
    [Test]
    public void Deconstruct_DeconstructsEntity()
    {
        using World world = new();

        var e = world.Create<Class1, Struct1, int, double, string>(new(), new(), 1, 2.0, "3");

        e.Deconstruct(
            out Ref<Class1> class1,
            out Ref<Struct1> struct1,
            out Ref<int> int1,
            out Ref<double> double1,
            out Ref<string> string1);

        That(class1.Value, Is.EqualTo(e.Get<Class1>()));
        That(struct1.Value, Is.EqualTo(e.Get<Struct1>()));
        That(int1.Value, Is.EqualTo(e.Get<int>()));
        That(double1.Value, Is.EqualTo(e.Get<double>()));
        That(string1.Value, Is.EqualTo(e.Get<string>()));
    }

    [Test]
    public void Deconstruct_ThrowsNoComponent()
    {
        using World world = new();

        var e = world.Create<Class1, Struct1, int, double, string>(new(), new(), 1, 2.0, "3");

        Throws<ComponentNotFoundException>(() => e.Deconstruct(out Ref<Class2> _));
    }
}
