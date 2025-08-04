using static NUnit.Framework.Assert;
using Frent.Collections;

namespace Frent.Tests.Collections;

public class BitsetTests
{
    [Test]
    public void SetOrResize_BitWithinCapacity_SetsBit()
    {
        var bitset = new Bitset(64);

        bitset.SetOrResize(10);

        That(bitset.IsSet(10), Is.True);
        That(bitset.IsSet(11), Is.False);
    }

    [Test]
    public void SetOrResize_BitExceedsCapacity_ResizesAndSetsBit()
    {
        var bitset = new Bitset(1);

        int index = 1024;
        bitset.SetOrResize(index);

        That(bitset.IsSet(index), Is.True);
        That(bitset.IsSet(index + 1), Is.False);
    }

    public void IsSet_BitPreviouslySet_ReturnsTrue([Values(1, 8, 128, 127, 40)] int index)
    {
        var bitset = new Bitset(index + 1);
        bitset.SetOrResize(index);

        That(bitset.IsSet(index), Is.True);
    }

    [Test]
    public void IsSet_BitNotSet_ReturnsFalse()
    {
        var bitset = new Bitset(64);

        That(bitset.IsSet(42), Is.False);
    }

    [Test]
    public void Enumerator_BitsSet_YieldsCorrectIndices()
    {
        var bitset = new Bitset(100);
        var expected = new[] { 1, 3, 5, 10, 63, 64, 128 };

        foreach (var index in expected)
            bitset.SetOrResize(index);

        var actual = new List<int>();
        var enumerator = new Bitset.Enumerator(bitset);

        while (enumerator.MoveNext())
            actual.Add(enumerator.Current);

        That(expected, Is.EqualTo(actual));
    }

    [Test]
    public void Enumerator_NoBitsSet_YieldsNothing()
    {
        var bitset = new Bitset(100);
        var enumerator = new Bitset.Enumerator(bitset);

        That(enumerator.MoveNext(), Is.False);
    }
}