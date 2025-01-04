using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NUnit.Framework.Assert;

namespace Frent.Tests;


[TestFixture]
public class EntityTests
{
    private World _world;
    private Entity _ent;

    [SetUp]
    public void Setup()
    {
        _world = new World();
        _ent = _world.Create<int, double, float>(69, 3.14, 2.71f);
    }

    [Test]
    public void EntityIsAlive_EntityIsAlive()
    {
        IsTrue(_ent.IsAlive());
    }

    [Test]
    public void EntityHas_ComponentPresent()
    {
        IsTrue(_ent.Has<int>());
    }

    [Test]
    public void EntityHas_ComponentNotPresent()
    {
        That(_ent.Has<bool>(), Is.False);
    }

    [Test]
    public void EntityAddComponent_AddsStringComponent()
    {
        _ent.Add<string>("I like Frent");
        That(_ent.Has<string>(), Is.True);
    }

    [Test]
    public void EntityTryGet_ComponentExists()
    {
        _ent.Add<string>("I like Frent");

        bool exists = _ent.TryGet<string>(out Ref<string> strRef);
        IsTrue(exists);
        That(strRef.Component, Is.EqualTo("I like Frent"));
    }

    [Test]
    public void EntityTryGet_ComponentDoesNotExist()
    {
        bool exists = _ent.TryGet(out Ref<bool> _);
        That(exists, Is.False);
    }

    [Test]
    public void EntityGet_ComponentExists()
    {
        _ent.Add<string>("I like Frent");

        string result = _ent.Get<string>();
        That(result, Is.EqualTo("I like Frent"));
    }

    [Test]
    public void EntityGet_ComponentDoesNotExist()
    {
        Throws<ComponentNotFoundException<string>>(() => _ent.Get<string>());
    }

    [Test]
    public void EntityDeconstruct_ValidComponents()
    {
        _ent.Add<string>("Hello World");

        _ent.Deconstruct(out Ref<double> d, out Ref<int> i, out Ref<float> f, out Ref<string> str);

        d.Component = 4;
        str.Component = "New Value";
        That(d.Component, Is.EqualTo(4));
        That(str.Component, Is.EqualTo("New Value"));
    }

    [Test]
    public void EntityDeconstruct_WithOneComponent()
    {
        _ent.Add<string>("Hello World");

        _ent.Deconstruct(out string str1);
        That(str1, Is.EqualTo("Hello World"));
    }

    [Test]
    public void EntityRemove_Component()
    {
        _ent.Add<string>("I like Frent");

        string removed = _ent.Remove<string>();
        That(removed, Is.EqualTo("I like Frent"));
        That(_ent.Has<string>(), Is.False);
    }

    [Test]
    public void EntityRemove_ComponentThatDoesNotExist()
    {
        Throws<ComponentNotFoundException>(() => _ent.Remove<string>());
    }

    [Test]
    public void EntityDelete_EntityIsDeleted()
    {
        _ent.Delete();
        That(_ent.IsAlive(), Is.False);
    }

    [Test]
    public void EntityIsNull_EntityIsNotNull()
    {
        That(_ent.IsNull, Is.False);
    }

    [Test]
    public void EntityComponentTypes_EntityHasComponentTypes()
    {
        _ent.Add<string>("Hello");

        var componentTypes = _ent.ComponentTypes;
        Contains(typeof(int), componentTypes);
        Contains(typeof(double), componentTypes);
        Contains(typeof(float), componentTypes);
        Contains(typeof(string), componentTypes);
    }

    [TearDown]
    public void Teardown()
    {
        _world.Dispose();
    }
}