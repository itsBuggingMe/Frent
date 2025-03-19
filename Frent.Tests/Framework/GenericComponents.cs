using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;
internal class GenericComponents
{
    [Test]
    public void GenericComponent_Updated()
    {
        using World world = new World();

        var e1 = world.Create<GenericComponent<int>>(default);
        var e2 = world.Create<GenericComponent<object>>(default);

        world.Update();

        That(e1.Get<GenericComponent<int>>().CalledCount, Is.EqualTo(1));
        That(e1.Has<GenericComponent<object>>(), Is.False);

        That(e2.Get<GenericComponent<object>>().CalledCount, Is.EqualTo(1));
        That(e2.Has<GenericComponent<int>>(), Is.False);
    }
}
