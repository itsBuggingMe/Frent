using Frent.Tests.Helpers;
using Frent.Core;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class TagTests
{
    [Test]
    public static void GetComponentID_Unqiue()
    {
        HashSet<TagID> componentIDs = new()
        {
            Tag.GetTagID(typeof(int)),
            Tag.GetTagID(typeof(long)),
            Tag.GetTagID(typeof(double)),   
            Tag.GetTagID(typeof(string)),
        };

        That(componentIDs.Count, Is.EqualTo(4));
    }

    [Test]
    public static void GetComponentID_Same()
    {
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        That(Tag.GetTagID(typeof(int)), Is.EqualTo(Tag.GetTagID(typeof(int))));
        That(Tag.GetTagID(typeof(Struct1)), Is.EqualTo(Tag.GetTagID(typeof(Struct1))));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument
    }

    [Test]
    public static void GetComponentIDGeneric_Unqiue()
    {
        HashSet<TagID> componentIDs = new()
        {
            Tag<int>.ID,
            Tag<long>.ID,
            Tag<double>.ID,
            Tag<string>.ID,
        };

        That(componentIDs.Count, Is.EqualTo(4));
    }

    [Test]
    public static void GetComponentIDGeneric_Same()
    {
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        That(Tag<int>.ID, Is.EqualTo(Tag.GetTagID(typeof(int))));
        That(Tag<Struct1>.ID, Is.EqualTo(Tag.GetTagID(typeof(Struct1))));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument
    }
}
