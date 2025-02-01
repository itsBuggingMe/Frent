using Frent.Systems;

namespace Frent.Tests.Helpers;

internal static class Extensions
{
    public static int EntityCount(this Query query)
    {
        int count = 0;
        query.RunEntity(e => count++);
        return count;
    }

    public static void AssertEntitiesNotDefault(this Query query)
    {
        query.RunEntity(e =>
        {
            foreach(var component in e.ComponentTypes)
            {
                AssertNotDefault(e.Get(component));
            }
        });

        static void AssertNotDefault(object value)
        {
            Type type = value.GetType();
            if (type.IsValueType)
            {
                Assert.That(value, Is.Not.EqualTo(Activator.CreateInstance(type)));
            }
            else
            {
                Assert.That(value, Is.Not.Null);
            }
        }
    }
}
