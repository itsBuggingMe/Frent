using Frent.Tests.Helpers;
using Frent.Core;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class ComponentTests
{
    [Test]
    public static void GetComponentID_Unqiue()
    {
        Component.RegisterComponent<int>();
        Component.RegisterComponent<long>();
        Component.RegisterComponent<double>();
        Component.RegisterComponent<string>();

        HashSet<ComponentID> componentIDs = new()
        {
            Component.GetComponentID(typeof(int)),
            Component.GetComponentID(typeof(long)),
            Component.GetComponentID(typeof(double)),
            Component.GetComponentID(typeof(string)),
        };

        That(componentIDs.Count, Is.EqualTo(4));
    }

    [Test]
    public static void GetComponentID_Same()
    {
        Component.RegisterComponent<int>();
        Component.RegisterComponent<Struct1>();

#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        That(Component.GetComponentID(typeof(int)), Is.EqualTo(Component.GetComponentID(typeof(int))));
        That(Component.GetComponentID(typeof(Struct1)), Is.EqualTo(Component.GetComponentID(typeof(Struct1))));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument
    }

    [Test]
    public static void GetComponentIDGeneric_Unqiue()
    {
        Component.RegisterComponent<int>();
        Component.RegisterComponent<long>();
        Component.RegisterComponent<double>();
        Component.RegisterComponent<string>();

        HashSet<ComponentID> componentIDs = new()
        {
            Component<int>.ID,
            Component<long>.ID,
            Component<double>.ID,
            Component<string>.ID,
        };

        That(componentIDs.Count, Is.EqualTo(4));
    }

    [Test]
    public static void GetComponentIDGeneric_Same()
    {
        Component.RegisterComponent<int>();
        Component.RegisterComponent<Struct1>();

#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        That(Component<int>.ID, Is.EqualTo(Component.GetComponentID(typeof(int))));
        That(Component<Struct1>.ID, Is.EqualTo(Component.GetComponentID(typeof(Struct1))));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument
    }
}
