using static NUnit.Framework.Assert;

namespace Frent.Tests.SparseComponents;

internal class EntityOperations
{
    [Test]
    public void AddRemove()
    {
        using World world = new World();

        Entity e = world.Create(new SparseComponent(null, world));

        ref SparseComponent comp = ref e.Get<SparseComponent>();
        That(comp.Data, Is.EqualTo(world));
    }
}
