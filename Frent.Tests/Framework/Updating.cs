using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using Frent.Updating;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;

internal class Updating
{
    [Test]
    public void ComponentFilter_UpdatesSingleComponent()
    {
        using World world = new();

        int count = 0;

        world.Create(0f, 0, new DelegateBehavior(() => count++));
        world.Create(0f, 0, new DelegateBehavior(() => count++));
        world.Create(new DelegateBehavior(() => count++));

        world.Create(new DelegateBehavior(() => count++), new FilteredBehavior1(() => count++));

        world.Create(0, new FilteredBehavior1(() => count++));

        world.UpdateComponent(Component<DelegateBehavior>.ID);

        That(count, Is.EqualTo(4));

        world.UpdateComponent(Component<DelegateBehavior>.ID);

        That(count, Is.EqualTo(8));

        world.UpdateComponent(Component<FilteredBehavior1>.ID);

        That(count, Is.EqualTo(10));
    }

    [Test]
    public void Update_UpdatesComponents()
    {
        using World world = new();

        int count = 0;
        for (int i = 0; i < 10; i++)
            world.Create<int, float, DelegateBehavior>(default, default, new DelegateBehavior(() => count++));

        world.Update();

        That(count, Is.EqualTo(10));
    }

    [Test]
    public void Update_FiltersComponents()
    {
        using World world = new();

        int count = 0;
        for (int i = 0; i < 10; i++)
            world.Create<int, float, FilteredBehavior1>(default, default, new FilteredBehavior1(() => count++));
        for (int i = 0; i < 10; i++)
            world.Create<int, float, FilteredBehavior2>(default, default, new FilteredBehavior2(() => count++));

        world.Update<FilterAttribute1>();

        That(count, Is.EqualTo(10));

        world.Update<FilterAttribute2>();

        That(count, Is.EqualTo(20));
    }

    [Test]
    public void Update_RegisterLate_FiltersComponents()
    {
        int count = 0;
        using World world = new();

        world.Update<FilterAttribute1>();

        for(int i = 0; i < 10; i++)
        {
            world.Create(new LazyComponent<int>(() => count++));
            world.Create(new FilteredBehavior2(() => count++));
        }

        world.Update<FilterAttribute1>();
        That(count, Is.EqualTo(10));

        for (int i = 0; i < 10; i++)
        {
            world.Create(new LazyComponent<double>(() => count++));
            world.Create(new FilteredBehavior2(() => count++));
        }

        world.Update<FilterAttribute1>();
        That(count, Is.EqualTo(30));
    }

    [Test]
    public void Update_DefaultConfig_DoesNotUpdateDeferredEntities()
    {
        int count = 0;
        using World world = new();

        world.Create(new DelegateBehavior(() =>
        {
            world.Create(new DelegateBehavior(() =>
            {
                count++;
            }));
        }));
        
        world.Update();

        That(count, Is.EqualTo(0));
        
        world.Update();

        That(count, Is.EqualTo(1));
    }

    [Test]
    public void Update_DeferredEntityCreationUpdate_UpdatesDeferredEntities()
    {
        int count = 0;
        using World world = new(null, true);

        world.Create(new DelegateBehavior(() =>
        {
            world.Create(new DelegateBehavior(() =>
            {
                count++;
            }));
        }));

        world.Update();

        That(count, Is.EqualTo(1));

        world.Update();

        That(count, Is.EqualTo(3));
    }

    [Test]
    public void Update_DeferredEntityCreationUpdate_HitsRecursionLimit()
    {
        using World world = new(null, true);

        world.Create(new DelegateBehavior(() =>
        {
            Create();
        }));

        Throws<InvalidOperationException>(() => world.Update());

        void Create()
        {
            world.Create(new DelegateBehavior(() =>
            {
                Create();
            }));
        }
    }

    [Test]
    public void Update_FilteredDeferredEntityCreationUpdate_UpdatesDeferredEntities()
    {
        int count = 0;
        using World world = new(null, true);

        world.Create(new FilteredBehavior1(() =>
        {
            world.Create(new FilteredBehavior1(() =>
            {
                count++;
            }));
        }));

        world.Update<FilterAttribute1>();

        That(count, Is.EqualTo(1));

        world.Update<FilterAttribute1>();

        That(count, Is.EqualTo(3));
    }

    [Test]
    public void Update_MultipleUpdateMethods_Filters()
    {
        using World world = new();

        Entity e = world.Create(new MultipleUpdateComponent());
        ref MultipleUpdateComponent comp = ref e.Get<MultipleUpdateComponent>();

        world.Update();

        That(comp.Count1, Is.EqualTo(1));
        That(comp.Count2, Is.EqualTo(1));

        world.Update<FilterAttribute1>();

        That(comp.Count1, Is.EqualTo(2));
        That(comp.Count2, Is.EqualTo(1));

        world.Update<FilterAttribute2>();

        That(comp.Count1, Is.EqualTo(2));
        That(comp.Count2, Is.EqualTo(2));
    }

    [Test]
    public void Update_SkipsEmptyArchetype()
    {
        using World world = new();

        Entity e = world.Create(new DependencyComponent());
        e.Delete();

        world.Update();
        world.Update<FilterAttribute1>();
    }

    [Test]
    public void DeferredUpdate_FilterNoMatch_NoNullReferenceException()
    {
        using World world = new();

        world.Create<NoMatch>(new(world));

        world.Update<FilterAttribute2>();
    }
}

internal partial struct LazyComponent<T>(Action a) : IUpdate
{
    [FilterAttribute1]
    public void Update() => a();
}

internal struct MultipleUpdateComponent : IUpdate, IUpdate<MultipleUpdateComponent>
{
    public int Count1;
    public int Count2;
    [FilterAttribute1]
    public void Update() => Count1++;
    [FilterAttribute2]
    public void Update(ref MultipleUpdateComponent _) => Count2++;
}

internal struct NoMatch(World world) : IUpdate
{
    [FilterAttribute2]
    public void Update()
    {
        world.Create(new LazyComponent<float>(() => throw new Exception("This should not called")));
    }
}