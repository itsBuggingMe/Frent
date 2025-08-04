using Frent.Core;
using Frent.Systems;
using Frent.Tests.Helpers;
using System.Runtime.CompilerServices;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class WorldTests
{
    //Naming convention
    //MethodName_IntendedBehavior

    [Test]
    public void Ctor_NewID()
    {
        using World world1 = new();

        ushort id;
        using (World world2 = new())
        {
            That(id = world2.WorldID, Is.Not.EqualTo(world1.WorldID));
        }
    }

    [Test]
    public void CreateEmpty_CreatesEntity()
    {
        using World world = new();

        var entity = world.Create();

        That(entity.IsAlive);

        That(entity.TagTypes, Is.Empty);
        That(entity.ComponentTypes, Is.Empty);
    }

    [Test]
    public void CreateFromObjects_CreatesEntity()
    {
        Component.RegisterComponent<int>();
        Component.RegisterComponent<Class1>();
        Component.RegisterComponent<Struct1>();
        CreateEntityTest(w => w.CreateFromObjects([6, new Class1(), new Struct1()]));
    }

    [Test]
    public void CreateGeneric_CreatesEntity()
    {
        CreateEntityTest(w => w.Create(6, new Class1(), new Struct1()));
    }

    [Test]
    public void EnsureCapacityGeneric_Allocates()
    {
        const int EntitiesToAllocate = 1000;
    
        using World world = new();
    
        Memory.Record();
        world.EnsureCapacity(EntityType.EntityTypeOf([Component<int>.ID, Component<long>.ID], []), EntitiesToAllocate);
        world.Create<int, long>(default, default).Delete();
        Memory.AllocatedAtLeast(EntitiesToAllocate * (sizeof(int) + sizeof(long) + Unsafe.SizeOf<EntityLocation>()));

        Memory.Record();
        for (int i = 0; i < EntitiesToAllocate - 1; i++) 
            world.Create<int, long>(default, default);
        Memory.NotAllocated();
    }

    [Test]
    public void Query_IncludesComponents()
    {
        using World world = new();

        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class1(), new Struct1(1));

        world.Create(1, new Class2(), new Struct2());

        var query = world.Query<int, Class1, Struct1>();

        query.AssertEntitiesNotDefault();

        That(query.EntityCount(), Is.EqualTo(3));
    }

    [Test]
    public void ComponentAdded_Invoked()
    {
        using World world = new();

        world.ComponentAdded += (e, c) => Pass();

        world.Create().Add<Struct1>(default);

        Fail();
    }

    [Test]
    public void ComponentRemoved_Invoked()
    {
        using World world = new();

        world.ComponentRemoved += (e, c) => Pass();

        world.Create<Struct1>(default).Remove<Struct1>();

        Fail();
    }

    [Test]
    public void EntityDelete_Invoked()
    {
        using World world = new();

        world.EntityDeleted += (e) => Pass();

        world.Create().Delete();

        Fail();
    }

    [Test]
    public void EntityCreated_Invoked()
    {
        using World world = new();

        world.EntityCreated += (e) => Pass();

        world.Create();

        Fail();
    }

    [Test]
    public void TagAdded_Invoked()
    {
        using World world = new();

        world.TagTagged += (e, t) => Pass();

        world.Create().Tag<Struct1>();

        Fail();
    }

    [Test]
    public void TagRemoved_Invoked()
    {
        using World world = new();

        world.TagDetached += (e, t) => Pass();

        var e = world.Create();
        e.Tag<Struct1>();
        e.Detach<Struct1>();

        Fail();
    }

    [Test]
    public void EntityCreated_DuringUpdate_AcsessComponents()
    {
        using World world = new();

        world.Create(new DelegateBehavior(() =>
        {
            bool has = Enumerable.Range(0, 100).Select(n => (n, Entity: world.Create(n))).ToArray().All(e => e.Entity.Get<int>() == e.n);

            That(has);

            int called = 0;

            var e = world.Create(new DelegateBehavior(() => called++));

            e.Get<DelegateBehavior>().Update();

            That(called, Is.EqualTo(1));
        }));

        world.Update();
    }

    [Test]
    public void EntityCreate_DuringSystem_AcsessComponents()
    {
        using World world = new();

        foreach (var entity in Enumerable.Range(0, 100).Select(i => world.Create(i, (float)(i * 2))).ToArray().Where(c => c.Get<int>() % 2 == 0))
        {//introduce a more complex internal state
            entity.Delete();
        }

        List<Entity> newEntities = [];

        foreach(Entity _ in world.Query<int, float>().EnumerateWithEntities())
        {
            var e1 = world.Create(42, 42f);
            var e2 = world.Create(42);

            That(e1.Get<int>(), Is.EqualTo(42));
            That(e1.Get<float>(), Is.EqualTo(42f));
            That(e1.ComponentTypes, Is.EquivalentTo((ComponentID[])[Component<int>.ID, Component<float>.ID]));

            That(e2.Get<int>(), Is.EqualTo(42));
            That(e2.ComponentTypes, Is.EquivalentTo((ComponentID[])[Component<int>.ID]));

            newEntities.Add(e1);
        }

        That(newEntities.All(e => e.Get<int>() == 42 && e.Get<float>() == 42));
    }


    #region Helpers
    private static void CreateEntityTest(Func<World, Entity> create)
    {
        using World world = new();
        var entity = create(world);

        That(entity.IsAlive);

        That(entity.ComponentTypes, Has.Length.EqualTo(3));
        That(entity.TagTypes, Is.Empty);

        That(entity.ComponentTypes, Does.Contain(Component<int>.ID));
        That(entity.ComponentTypes, Does.Contain(Component<Class1>.ID));
        That(entity.ComponentTypes, Does.Contain(Component<Struct1>.ID));

        That(entity.Get<int>(), Is.EqualTo(6));
    }
    #endregion
}
