using Frent.Tests.Helpers;
using Frent.Core;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class EntityTests
{
    [Test]
    public void Ctor_CreatesNull()
    {
        That(new Entity(), Is.EqualTo(Entity.Null));
        That(new Entity(), Is.EqualTo(default(Entity)));
    }

    [Test]
    public void OnComponentAddedGeneric_Invoked()
    {
        using World world = new();

        var entity = world.Create();
        entity.OnComponentAddedGeneric += new GenericAction((t, o) =>
        {
            That(o, Is.EqualTo(1));
            if (t == typeof(int))
                Pass();
            Fail();
        });

        entity.Add(1);
    }

    [Test]
    public void OnComponentRemovedGeneric_Invoked()
    {
        using World world = new();

        var entity = world.Create(1);
        entity.OnComponentRemovedGeneric += new GenericAction((t, o) =>
        {
            That(o, Is.EqualTo(1));

            if (t == typeof(int))
                Pass();
            Fail();
        });

        entity.Remove<int>();
    }

    [Test]
    public void OnComponentAdded_Invoked()
    {
        using World world = new();

        var entity = world.Create();
        entity.OnComponentAdded += (t, o) =>
        {
            That(t.Get<int>(), Is.EqualTo(1));
            if (o.Type == typeof(int))
                Pass();
            Fail();
        };

        entity.Add(1);
    }

    [Test]
    public void OnComponentRemoved_Invoked()
    {
        using World world = new();

        var entity = world.Create(1);
        entity.OnComponentRemoved += (t, o) =>
        {
            if (o.Type == typeof(int))
                Pass();
            Fail();
        };

        entity.Remove<int>();
    }

    [Test]
    public void OnTagged_Invoked()
    {
        using World world = new();
        var entity = world.Create(1);
        entity.OnTagged += (a, b) => Pass();
        entity.Tag<int>();
        Fail();
    }

    [Test]
    public void OnDetach_Invoked()
    {
        using World world = new();
        var entity = world.Create(1);
        entity.OnDetach += (a, b) => Pass();
        entity.Tag<int>();
        Fail();
    }

    [Test]
    public void OnDelete_Invoked()
    {
        using World world = new();
        var entity = world.Create(1);
        entity.OnDelete += (a) => Pass();
        entity.Delete();
        Fail();
    }

    [Test]
    public void World_IsWorld()
    {
        using World world = new();
        var e = world.Create();
        That(e.World, Is.EqualTo(world));
    }

    [Test]
    public void AddAs_AddsAs()
    {
        using World world = new();
        var e = world.Create();
        e.Add(Component<BaseClass>.ID, new ChildClass());

        That(e.Get<BaseClass>().GetType(), Is.EqualTo(typeof(ChildClass)));
        Throws<InvalidCastException>(() => e.Add(Component<ChildClass>.ID, new BaseClass()));
    }

    [Test]
    public void Add_DefaultType()
    {
        using World world = new();
        var e = world.Create();
        e.Add((object)1);

        That(e.Has<int>());
        That(e.Get<int>(), Is.EqualTo(1));
    }

    [Test]
    public void AddAsType_AddsAsType()
    {
        Component.RegisterComponent<BaseClass>();
        Component.RegisterComponent<ChildClass>();
        Component.RegisterComponent<Class1>();

        using World world = new();
        var e = world.Create();
        e.Add(typeof(ChildClass), new ChildClass());

        That(e.Get<ChildClass>().GetType(), Is.EqualTo(typeof(ChildClass)));
        Throws<InvalidCastException>(() => e.Add(typeof(Class1), new Class2()));
    }

    [Test]
    public void Delete_NoLongerIsAlive()
    {
        using World world = new();
        var e = world.Create(new Struct1(), new Struct2(), new Struct3());

        That(e.IsAlive, Is.True);
        e.Delete();
        That(e.IsAlive, Is.False);
    }

    [Test]
    public void Detach_RemovesTag()
    {
        using World world = new();
        var e = world.Create(0, 0.0, "0");
        e.Tag<Struct1>();
        e.Tag<Struct2>();
        e.Tag<Struct3>();

        e.Detach<Struct1>();
        e.Detach<Struct2>();
        e.Detach<Struct3>();

        That(e.TagTypes, Is.Empty);
    }

    [Test]
    public void Tag_AddsTag()
    {
        using World world = new();
        var e = world.Create(0, 0.0, "0");
        e.Tag<Struct1>();
        e.Tag<Struct2>();
        e.Tag<Struct3>();

        That(e.TagTypes, Has.Length.EqualTo(3));
        That(e.TagTypes, Does.Contain(Tag<Struct1>.ID));
        That(e.TagTypes, Does.Contain(Tag<Struct2>.ID));
        That(e.TagTypes, Does.Contain(Tag<Struct3>.ID));
    }

    [Test]
    public void EnumerateComponents_IteratesAllComponents()
    {
        using World world = new();
        var e = world.Create(new Struct1(), new Struct2(), new Struct3());

        List<Type> types = [];
        e.EnumerateComponents(new GenericAction((t, o) => types.Add(t)));

        That(types, Is.EqualTo(new[] { typeof(Struct1), typeof(Struct2), typeof(Struct3) }));
        That(types, Is.EqualTo(e.ComponentTypes.Select(t => t.Type)));
    }

    [Test]
    public void GetGeneric_ReturnsReference()
    {
        using World world = new();
        var e = world.Create(10, new Struct1(), new Struct2(), new Class1());

        That(e.Get<int>(), Is.EqualTo(10));

        e.Get<int>() = 20;

        That(e.Get<int>(), Is.EqualTo(20));
    }

    [Test]
    public void Get_ReturnsComponent()
    {
        using World world = new();
        var e = world.Create(10, new Struct1(), new Struct2(), new Class1());

        That(e.Get(typeof(int)), Is.EqualTo(10));
        That(e.Get(Component<int>.ID), Is.EqualTo(10));
    }

    [Test]
    public void Has_ReturnsTrueIfHasComponent()
    {
        using World world = new();
        var e = world.Create(10, new Struct1(), new Struct2(), new Class1());

        That(e.Has(typeof(int)), Is.True);
        That(e.Has(Component<int>.ID), Is.True);

        That(e.Has(typeof(double)), Is.False);
        That(e.Has(Component<double>.ID), Is.False);

        That(e.Has<Struct1>(), Is.True);
        That(e.Has<Struct2>(), Is.True);
        That(e.Has<Class1>(), Is.True);
    }

    [Test]
    public void Remove_RemovesComponent()
    {
        using World world = new();
        var e = world.Create(new Struct1(), new Struct2(), new Struct3());

        e.Remove<Struct1>();
        e.Remove(Component<Struct2>.ID);

        That(e.ComponentTypes, Has.Length.EqualTo(1));
    }

    [Test]
    public void Set_ChangesObjectValue()
    {
        using World world = new();
        var e = world.Create(-1, new Struct1(-2));

        That(e.Get<int>(), Is.EqualTo(-1));
        That(e.Get<Struct1>().Value, Is.EqualTo(-1));

        e.Set(Component<int>.ID, 1);
        That(e.Get<int>(), Is.EqualTo(1));

        e.Set(typeof(Struct1), 1);
        That(e.Get<Struct1>().Value, Is.EqualTo(1));
    }

    [Test]
    public void Tagged_ChecksTag()
    {
        using World world = new();
        var e = world.Create();
        e.Tag<Struct1>();

        That(e.Tagged<int>(), Is.False);
        e.Tag<int>();
        That(e.Tagged<int>(), Is.True);
        That(e.Tagged(Tag<Struct1>.ID), Is.True);
    }

    [Test]
    public void TryGet_ReturnsFalseNoComponent()
    {
        using World world = new();

        var e = world.Create(new Struct1(1));

        That(e.TryGet<int>(out var value), Is.False);
    }

    [Test]
    public void TryGet_ThrownWhenDead()
    {
        using World world = new();

        var e = world.Create(new Struct1(2));
        e.Delete();

        Throws<InvalidOperationException>(() => e.TryGet<Struct1>(out var value));
    }

    [Test]
    public void TryGet_ReturnsCorrectRef()
    {
        using World world = new();

        var e = world.Create(new Struct1(3));

        That(e.TryGet<Struct1>(out var value), Is.True);
        That(value.Value.Value, Is.EqualTo(3));
        value.Value.Value = 1;
        //value value value value value value value value

        That(e.Get<Struct1>().Value, Is.EqualTo(1));
    }

    [Test]
    public void TryGet_DoesntThrow()
    {
        using World world = new();

        var e = world.Create(new Struct1(4));
        e.Delete();

        That(e.TryGet<Struct1>(out _), Is.False);
    }

    [Test]
    public void TryHas_DoesntThrow()
    {
        using World world = new();

        var e = world.Create(new Struct1(4));
        e.Delete();

        That(e.TryHas<Struct1>(), Is.False);
    }

    [Test]
    public void TryHas_ReturnsTrue()
    {
        using World world = new();

        var e = world.Create(new Struct1(4));

        That(e.TryHas<Struct1>(), Is.True);
    }

    internal class GenericAction(Action<Type, object?> onAction) : IGenericAction<Entity>, IGenericAction
    {
        public void Invoke<T>(Entity e, ref T type) => onAction(typeof(T), type);
        public void Invoke<T>(ref T type) => onAction(typeof(T), type);
    }
}
