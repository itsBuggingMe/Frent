using Frent.Collections;

namespace Frent.Tests.Collections;

public class ShortSparseSetTests
{
    [Test]
    public void Indexer_AddsValues_AndExposesDenseSpan()
    {
        ShortSparseSet<int> set = new();

        set[1] = 10;
        set[5] = 50;

        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set.AsSpan().ToArray(), Is.EqualTo(new[] { 10, 50 }));
        Assert.That(set.TryGet(1, out int first), Is.True);
        Assert.That(first, Is.EqualTo(10));
        Assert.That(set.TryGet(5, out int second), Is.True);
        Assert.That(second, Is.EqualTo(50));
    }

    [Test]
    public void Remove_MovesLastValue_AndUpdatesSparseMapping()
    {
        ShortSparseSet<string> set = new();

        set[1] = "one";
        set[2] = "two";
        set[3] = "three";

        set.Remove(2);

        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set.TryGet(2, out _), Is.False);
        Assert.That(set.TryGet(3, out string? moved), Is.True);
        Assert.That(moved, Is.EqualTo("three"));
        Assert.That(set.AsSpan().ToArray(), Is.EqualTo(new[] { "one", "three" }));
    }

    [Test]
    public void Remove_ClearsSparseSlot_AndAllowsReadd()
    {
        ShortSparseSet<int> set = new();

        set[7] = 70;
        set.Remove(7);
        set[7] = 700;

        Assert.That(set.Count, Is.EqualTo(1));
        Assert.That(set.TryGet(7, out int value), Is.True);
        Assert.That(value, Is.EqualTo(700));
        Assert.That(set.AsSpan().ToArray(), Is.EqualTo(new[] { 700 }));
    }

    [Test]
    public void Clear_RemovesAllSparseMappings()
    {
        ShortSparseSet<int> set = new();

        set[1] = 10;
        set[2] = 20;

        set.Clear();

        Assert.That(set.Count, Is.EqualTo(0));
        Assert.That(set.TryGet(1, out _), Is.False);
        Assert.That(set.TryGet(2, out _), Is.False);
        Assert.That(set.AsSpan().Length, Is.EqualTo(0));
    }
}
