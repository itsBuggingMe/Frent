using Frent.Components;
using Frent.Tests.Helpers;
using NUnit.Framework.Constraints;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;
internal class Lifetime
{
    [Test]
    public void DestroyCalled_OnDelete()
    {
        using World world = new();
        int destroyCount = 0;

        List<Entity> entities = new List<Entity>();

        for (int i = 0; i < 10; i++)
        {

            var entity = world.Create<int, float, FilteredBehavior1>(default, default, new FilteredBehavior1(() => { }, null, () => destroyCount++));

            entities.Add(entity);
        }

        foreach (var entity in entities)
        {
            entity.Delete();
        }

        That(destroyCount, Is.EqualTo(10));
    }

    [Test]
    public void InitCalled_WhenCreated()
    {
        using World world = new();

        for (int i = 0; i < 10; i++)
        {
            Entity e1 = default;

            var entity = world.Create<int, float, FilteredBehavior1>(default, default, new FilteredBehavior1(() => { }, e => e1 = e));

            That(e1, Is.EqualTo(entity));
        }
    }

    [Test]
    public void LifetimeCalled_AddRemove()
    {
        using World world = new();

        TestForLifetimeInvocation(world, (w, c) =>
        {
            Entity e = w.Create();
            e.Add(c);
            e.Remove<LifetimeComponent>();
        });
    }

    [Test]
    public void LifetimeCalled_CreateDelete()
    {
        using World world = new();

        TestForLifetimeInvocation(world, (w, c) =>
        {
            Entity e = w.Create(c);
            e.Delete();
        });
    }

    [Test]
    public void DestroyCalled_OnWorldDispose()
    {
        World world = new();

        TestForLifetimeInvocation(world, (w, c) =>
        {
            Entity e = w.Create(c);
            w.Dispose();
        });
    }

    private void TestForLifetimeInvocation(World world, Action<World, LifetimeComponent> action)
    {
        bool initFlag = false;
        bool destroyFlag = false;
        
        action(world, new LifetimeComponent(e => initFlag = true, e => destroyFlag = true));

        That(initFlag, Is.True);
        That(destroyFlag, Is.True);
    }

    internal struct LifetimeComponent(Action<Entity>? init, Action<Entity>? destroy) : IInitable, IDestroyable
    {
        private Entity _self;
        public void Init(Entity self) => init?.Invoke(_self = self);
        public void Destroy() => destroy?.Invoke(_self);
    }
}
