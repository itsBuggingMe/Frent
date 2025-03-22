using Frent.Core;
using Frent.Systems;
using Frent.Tests.Helpers;
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
            That(id = world2.ID, Is.Not.EqualTo(world1.ID));
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
    public void CustomQuery_CustomRuleApplies()
    {
        using World world = new();

        Query query = world.CustomQuery(Rule.Delegate(e => e.Types.Length == 3));

        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class2(), new Struct2(1));
        world.Create(1, new Class1());

        That(query.EntityCount(), Is.EqualTo(2));

        query.AssertEntitiesNotDefault();
    }

    [Test]
    public void CustomQuery_RuleApplies()
    {
        using World world = new();

        Query query = world.CustomQuery(
            Rule.HasComponent(Component<int>.ID),
            Rule.HasComponent(Component<Class1>.ID),
            Rule.HasComponent(Component<Struct1>.ID));

        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class2(), new Struct2(1));
        world.Create(1, new Class1());

        That(query.EntityCount(), Is.EqualTo(1));

        query.AssertEntitiesNotDefault();
    }

    [Test]
    public void CustomQuery_OverEmptyArchetype()
    {
        using World world = new();

        Query query = world.CustomQuery(Rule.IncludeDisabledRule);

        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class2(), new Struct2(1));
        world.Create(1, new Class1());

        That(query.EntityCount(), Is.EqualTo(3));

        query.AssertEntitiesNotDefault();
    }

    //[Test]
    //public void EnsureCapacityGeneric_Allocates()
    //{
    //    const int EntitiesToAllocate = 1000;
    //
    //    using World world = new();
    //
    //    Memory.Record();
    //    world.EnsureCapacity<int, long>(EntitiesToAllocate);
    //    Memory.AllocatedAtLeast(EntitiesToAllocate * (sizeof(int) + sizeof(long) + 10 /*size of entity table item*/));
    //
    //    Memory.Record();
    //    for(int i = 0; i < EntitiesToAllocate; i++) 
    //        world.Create<int, long>(default, default);
    //    Memory.NotAllocated();
    //}

    //[Test]
    //public void EnsureCapacity_Allocates()
    //{
    //    const int EntitiesToAllocate = 1000;
    //
    //    using World world = new();
    //
    //    Memory.Record();
    //
    //    world.EnsureCapacity([Component<int>.ID, Component<long>.ID], EntitiesToAllocate);
    //
    //    Memory.AllocatedAtLeast(EntitiesToAllocate * (sizeof(int) + sizeof(long)));
    //}

    [Test]
    public void Query_IncludesComponents()
    {
        using World world = new();

        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class1(), new Struct1(1));
        world.Create(1, new Class1(), new Struct1(1));

        world.Create(1, new Class2(), new Struct2());

        var query = world.Query<With<int>, With<Class1>, With<Struct1>>();

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
