using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;
internal class Lifetime
{
    [Test]
    public void Destroy_CalledOnDelete()
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
    public void Init_CalledWhenCreated()
    {
        using World world = new();

        for (int i = 0; i < 10; i++)
        {
            Entity e1 = default;

            var entity = world.Create<int, float, FilteredBehavior1>(default, default, new FilteredBehavior1(() => { }, e => e1 = e));

            That(e1, Is.EqualTo(entity));
        }
    }
}
