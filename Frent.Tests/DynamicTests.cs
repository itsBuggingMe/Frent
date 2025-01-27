using Frent.Core;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class DynamicTests
{
    [Test]
    public void TagID_Default()
    {
        That(default(TagID).Type, Is.EqualTo(typeof(Disable)));
    }

    [Test]
    public void ComponentID_Default()
    {
        That(default(ComponentID).Type, Is.EqualTo(typeof(void)));
    }

    [Test]
    public void EntityType_Default()
    {
        That(default(EntityType).Types.Length, Is.Zero);
        That(default(EntityType).Tags.Length, Is.Zero);
    }
}