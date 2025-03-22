﻿using Frent.Components;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;

internal class Updating
{
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
}

internal partial struct LazyComponent<T>(Action a) : IComponent
{
    [FilterAttribute1]
    public void Update() => a();
}