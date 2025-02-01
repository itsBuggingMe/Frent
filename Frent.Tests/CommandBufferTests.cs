using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class CommandBufferTests
{
    [Test]
    public void Create_CreatesEntityOnPlayback()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        Entity e = commandBuffer.Entity()
            .With<Struct1>(default)
            .With<Struct2>(default)
            .With<Struct3>(default)
            .End();

        That(e.ComponentTypes, Is.Empty);

        commandBuffer.PlayBack();

        That(e.ComponentTypes, Does.Contain(Component<Struct1>.ID));
        That(e.ComponentTypes, Does.Contain(Component<Struct2>.ID));
        That(e.ComponentTypes, Does.Contain(Component<Struct3>.ID));
    }

    [Test]
    public void HasBufferItem_ReturnsTrueIfBufferItemExists()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        That(commandBuffer.HasBufferItems, Is.False);

        commandBuffer.Entity()
            .With<Struct1>(default)
            .With<Struct2>(default)
            .With<Struct3>(default)
            .End();

        That(commandBuffer.HasBufferItems, Is.True);
    }

    [Test]
    public void Clear_ClearsBufferItems()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        var e = commandBuffer.Entity()
            .With<Struct1>(default)
            .With<Struct2>(default)
            .With<Struct3>(default)
            .End();

        commandBuffer.AddComponent(e, new Class1());

        commandBuffer.Clear();

        That(commandBuffer.HasBufferItems, Is.False);
        That(world.EntityCount, Is.Zero);
    }

    [Test]
    public void AddComponentGeneric_AddsComponentOnPlayback()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        var e1 = commandBuffer.Entity()
            .End();

        var e2 = world.Create();

        commandBuffer.AddComponent(e1, new Class1());
        commandBuffer.AddComponent(e2, new Class2());

        commandBuffer.PlayBack();

        That(e1.ComponentTypes, Does.Contain(Component<Class1>.ID));
        That(e2.ComponentTypes, Does.Contain(Component<Class2>.ID));

        That(e1.Get<Class1>(), Is.Not.Null);
        That(e2.Get<Class2>(), Is.Not.Null);
    }

    [Test]
    public void AddComponentAs_AddsComponentOnPlayback()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        var e1 = commandBuffer.Entity()
            .End();

        commandBuffer.AddComponent(e1, Component<BaseClass>.ID, new ChildClass());

        commandBuffer.PlayBack();

        That(e1.ComponentTypes, Does.Contain(Component<BaseClass>.ID));
        That(e1.ComponentTypes, Does.Not.Contain(Component<ChildClass>.ID));

        That(e1.Get<BaseClass>(), Is.Not.Null);
    }

    [Test]
    public void DeleteEntity_DeletesEntity()
    {
        using World world = new World();
        List<Entity> entities = new();
        for(int i = 0; i < 100; i++)
            entities.Add(world.Create(new Struct1(), new Struct2(), new Class1()));

        CommandBuffer commandBuffer = new CommandBuffer(world);

        foreach (var item in entities)
        {
            commandBuffer.DeleteEntity(item);
        }

        commandBuffer.PlayBack();
    }

    [Test]
    public void RemoveComponentGeneric_RemoveComponentOnPlayback()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        var e1 = commandBuffer.Entity()
            .With<Class1>(new())
            .End();

        var e2 = world.Create(new Class2());

        commandBuffer.RemoveComponent<Class1>(e1);
        commandBuffer.RemoveComponent<Class2>(e2);

        commandBuffer.PlayBack();

        That(e1.ComponentTypes, Does.Not.Contain(Component<Class1>.ID));
        That(e2.ComponentTypes, Does.Not.Contain(Component<Class2>.ID));
    }

    [Test]
    public void RemoveComponentAs_RemoveComponentOnPlayback()
    {
        using World world = new World();
        CommandBuffer commandBuffer = new CommandBuffer(world);

        var e1 = commandBuffer.Entity()
            .With<BaseClass>(new ChildClass())
            .End();

        commandBuffer.RemoveComponent(e1, Component<BaseClass>.ID);

        commandBuffer.PlayBack();

        That(e1.ComponentTypes, Does.Not.Contain(Component<BaseClass>.ID));
    }
}
