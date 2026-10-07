using System.Runtime.CompilerServices;
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
    public void DefaultRef_ChecksNull()
    {
        Ref<string> referenceTypeRef = default;
        Ref<int> valueTypeRef = default;

        That(referenceTypeRef.ToString(), Is.Null);
        That(valueTypeRef.ToString(), Is.Null);
    }

    [Test]
    public void OnComponentAddedGeneric_Invoked()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create();
        entity.OnComponentAddedGeneric += new GenericAction((t, o) =>
        {
            That(o, Is.EqualTo(1));
            if (t == typeof(int))
                call.Call();
        });

        entity.Add(1);
        call.AssertCalled();
    }


    [Test]
    public void OnComponentAddedGeneric_Invoked_AddAs()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create();
        entity.OnComponentAddedGeneric += new GenericAction((t, o) =>
        {
            That(o, Is.EqualTo(1));
            if (t == typeof(int))
                call.Call();
        });

        entity.AddAs(Component<int>.ID, 1);
        call.AssertCalled();
    }

    [Test]
    public void OnComponentRemovedGeneric_Invoked()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create(1);
        entity.OnComponentRemovedGeneric += new GenericAction((t, o) =>
        {
            That(o, Is.EqualTo(1));

            if (t == typeof(int))
                call.Call();
        });

        entity.Remove<int>();
        call.AssertCalled();
    }

    [Test]
    public void OnComponentAdded_Invoked()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create();
        entity.OnComponentAdded += (t, o) =>
        {
            That(t.Get<int>(), Is.EqualTo(1));
            if (o.Type == typeof(int))
                call.Call();
        };

        entity.Add(1);
        call.AssertCalled();
    }

    [Test]
    public void OnComponentAdded_Invoked_AddAs()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create();
        entity.OnComponentAdded += (t, o) =>
        {
            That(t.Get<int>(), Is.EqualTo(1));
            if (o.Type == typeof(int))
                call.Call();
        };

        entity.AddAs(Component<int>.ID, 1);
        call.AssertCalled();
    }

    [Test]
    public void OnComponentRemoved_Invoked()
    {
        using World world = new();
        CallHelper call = new();

        var entity = world.Create(1);
        entity.OnComponentRemoved += (t, o) =>
        {
            if (o.Type == typeof(int))
                call.Call();
        };

        entity.Remove<int>();
        call.AssertCalled();
    }

    [Test]
    public void OnTagged_Invoked()
    {
        using World world = new();
        CallHelper call = new();
        var entity = world.Create(1);
        entity.OnTagged += (a, b) => call.Call();
        entity.Tag<int>();
        call.AssertCalled();
    }

    [Test]
    public void OnDetach_Invoked()
    {
        using World world = new();
        CallHelper call = new();
        var entity = world.Create(1);
        entity.OnDetach += (a, b) => call.Call();
        entity.Tag<int>();
        entity.Detach<int>();
        call.AssertCalled();
    }

    [Test]
    public void OnDelete_Invoked()
    {
        using World world = new();
        CallHelper call = new();
        var entity = world.Create(1);
        entity.OnDelete += (a) => call.Call();
        entity.Delete();
        call.AssertCalled();
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
        e.AddAs(Component<BaseClass>.ID, new ChildClass());

        That(e.Get<BaseClass>().GetType(), Is.EqualTo(typeof(ChildClass)));
        Throws<InvalidCastException>(() => e.AddAs(Component<ChildClass>.ID, new BaseClass()));
    }

    [Test]
    public void Add_DefaultType()
    {
        using World world = new();
        var e = world.Create();
        e.AddBoxed((object)1);

        That(e.Has<int>());
        That(e.Get<int>(), Is.EqualTo(1));
    }

    [Test]
    public void AddMultiple_Deferred_AddsComponents()
    {
        using World world = new();
        var e = world.Create();

        foreach (var _ in world.CreateQuery()
            .Build()
            .EnumerateWithEntities())
        {
            e.Add(1, 2f);
        }

        That(e.Get<int>(), Is.EqualTo(1));
        That(e.Get<float>(), Is.EqualTo(2));
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
        Throws<InvalidCastException>(() => e.AddAs(typeof(Class1), new Class2()));
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
    public void ImplicitOperator_TracksIsAlive()
    {
        using World world = new();
        var e = world.Create(new Struct1(), new Struct2(), new Struct3());
        if(!e)
        {
            Fail("Expected entity to be alive");
        }
        e.Delete();
        if (e)
        {
            Fail("Expected entity to be dead");
        }
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
    public void DetachNonGeneric_DuringEnumeration_Defers()
    {
        using World world = new();
        var e = world.Create();
        e.Tag<Struct1>();

        foreach (var _ in world.CreateQuery().Build().EnumerateWithEntities())
        {
            That(e.Detach(Tag<Struct1>.ID), Is.True);
            That(e.Tagged<Struct1>(), Is.True);
        }

        That(e.Tagged<Struct1>(), Is.False);
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
    public void RemoveMany_RetainValue()
    {
        using World world = new();
        var e = world.Create(69, 42.0, new Struct1(), new Struct2(), new Struct3());

        e.Remove<int, Struct1, Struct2, Struct3>();

        That(e.Get<double>(), Is.EqualTo(42.0));
        That(e.ComponentTypes.Length, Is.EqualTo(1));
    }

    [Test]
    public void Set_ChangesObjectValue()
    {
        using World world = new();
        var e = world.Create(-1, new Struct1(-2));

        That(e.Get<int>(), Is.EqualTo(-1));
        That(e.Get<Struct1>().Value, Is.EqualTo(-2));

        e.Set(Component<int>.ID, 1);
        That(e.Get<int>(), Is.EqualTo(1));

        e.Set(typeof(Struct1), new Struct1(1));
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

    [Test]
    public void ComponentTagID_SameID_DoesNotInterfere()
    {
        // see #25

        using World world = new();

        // ensure at least 1 tag/comp type init
        var dummy = world.Create();
        dummy.Add(0);
        dummy.Tag<int>();
        dummy.Delete();

        Entity e = world.Create();

        TagID tag = new TagID(1);
        ComponentID component = new ComponentID(1);

        e.AddAs(component, Activator.CreateInstance(component.Type)!);

        That(e.Tagged(tag), Is.False);

        e.Tag(tag);

        That(e.Tagged(tag), Is.True);
    }

    [Test]
    public void GenericAddRemove_MoreSourceArchetypesThanCache()
    {
        using World world = new();
        Entity[] entities = CreateMarkerSpread(world, 64);

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < entities.Length; i++)
                entities[i].Add(new AddedComponent { V = i + round });

            for (int i = 0; i < entities.Length; i++)
            {
                That(entities[i].Has<AddedComponent>(), Is.True);
                That(entities[i].Get<AddedComponent>().V, Is.EqualTo(i + round));
                AssertMarkerEntity(entities[i], i);
            }

            for (int i = 0; i < entities.Length; i++)
                entities[i].Remove<AddedComponent>();

            for (int i = 0; i < entities.Length; i++)
            {
                That(entities[i].Has<AddedComponent>(), Is.False);
                AssertMarkerEntity(entities[i], i);
            }
        }
    }

    [Test]
    public void GenericAddRemove_MoreSourceArchetypesThanCache_TwoWorlds()
    {
        using World world1 = new();
        using World world2 = new();
        Entity[] first = CreateMarkerSpread(world1, 64);
        Entity[] second = CreateMarkerSpread(world2, 64);

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < first.Length; i++)
            {
                first[i].Add(new AddedComponent { V = i + round });
                second[i].Add(new AddedComponent { V = i + round + 1000 });
            }

            for (int i = 0; i < first.Length; i++)
            {
                That(first[i].Has<AddedComponent>(), Is.True);
                That(first[i].Get<AddedComponent>().V, Is.EqualTo(i + round));
                That(second[i].Has<AddedComponent>(), Is.True);
                That(second[i].Get<AddedComponent>().V, Is.EqualTo(i + round + 1000));
                AssertMarkerEntity(first[i], i);
                AssertMarkerEntity(second[i], i);
            }

            for (int i = 0; i < first.Length; i++)
            {
                first[i].Remove<AddedComponent>();
                second[i].Remove<AddedComponent>();
            }

            for (int i = 0; i < first.Length; i++)
            {
                That(first[i].Has<AddedComponent>(), Is.False);
                That(second[i].Has<AddedComponent>(), Is.False);
                AssertMarkerEntity(first[i], i);
                AssertMarkerEntity(second[i], i);
            }
        }
    }

    [Test]
    public void GenericTagDetach_MoreSourceArchetypesThanCache()
    {
        using World world = new();
        Entity[] entities = CreateMarkerSpread(world, 64);

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < entities.Length; i++)
                entities[i].Tag<CacheTag>();

            for (int i = 0; i < entities.Length; i++)
            {
                That(entities[i].Tagged<CacheTag>(), Is.True);
                AssertMarkerEntity(entities[i], i);
            }

            for (int i = 0; i < entities.Length; i++)
                entities[i].Detach<CacheTag>();

            for (int i = 0; i < entities.Length; i++)
            {
                That(entities[i].Tagged<CacheTag>(), Is.False);
                AssertMarkerEntity(entities[i], i);
            }
        }
    }

    private static Entity[] CreateMarkerSpread(World world, int count)
    {
        Entity[] entities = new Entity[count];
        for (int i = 0; i < entities.Length; i++)
        {
            CommonComponent common = new(i);
            entities[i] = (i % 8) switch
            {
                0 => world.Create(common, new Marker0()),
                1 => world.Create(common, new Marker1()),
                2 => world.Create(common, new Marker2()),
                3 => world.Create(common, new Marker3()),
                4 => world.Create(common, new Marker4()),
                5 => world.Create(common, new Marker5()),
                6 => world.Create(common, new Marker6()),
                _ => world.Create(common, new Marker7()),
            };
        }
        return entities;
    }

    private static void AssertMarkerEntity(Entity e, int i)
    {
        That(e.Get<CommonComponent>().Value, Is.EqualTo(i));
        That((i % 8) switch
        {
            0 => e.Has<Marker0>(),
            1 => e.Has<Marker1>(),
            2 => e.Has<Marker2>(),
            3 => e.Has<Marker3>(),
            4 => e.Has<Marker4>(),
            5 => e.Has<Marker5>(),
            6 => e.Has<Marker6>(),
            _ => e.Has<Marker7>(),
        }, Is.True);
    }

    private struct AddedComponent { public int V; }
    private record struct CommonComponent(int Value);
    private struct Marker0;
    private struct Marker1;
    private struct Marker2;
    private struct Marker3;
    private struct Marker4;
    private struct Marker5;
    private struct Marker6;
    private struct Marker7;
    private struct CacheTag;

    internal class GenericAction(Action<Type, object?> onAction) : IGenericAction<Entity>, IGenericAction
    {
        public void Invoke<T>(Entity e, ref T type) => onAction(typeof(T), type);
        public void Invoke<T>(ref T type) => onAction(typeof(T), type);
    }
}
