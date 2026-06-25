using Frent.Collections;
using System.Runtime.CompilerServices;

namespace Frent.Tests.Collections;

[TestFixture]
public class RefDictionaryTests
{
    private RefDictionary<string, int> _dict;

    [SetUp]
    public void Setup()
    {
        _dict = new RefDictionary<string, int>();
    }

    [Test]
    public void Add_NewKey_ReturnsFalseAndAddsValue()
    {
        bool exists;
        ref int valueRef = ref _dict.GetValueRefOrAddDefault("key1", out exists);

        Assert.That(exists, Is.False);
        valueRef = 42;

        ref int fetchedRef = ref _dict.GetValueRefOrAddDefault("key1", out exists);
        Assert.That(exists);
        Assert.That(fetchedRef, Is.EqualTo(42));
    }

    [Test]
    public void GetValueRefOrNullRef_ExistingKey_ReturnsRef()
    {
        ref int valueRef = ref _dict.GetValueRefOrAddDefault("key2", out bool exists);
        valueRef = 100;

        ref int found = ref _dict.GetValueRefOrNullRef("key2");
        Assert.IsFalse(Unsafe.IsNullRef(ref found));
        Assert.That(found, Is.EqualTo(100));
    }

    [Test]
    public void GetValueRefOrNullRef_NonExistingKey_ReturnsNullRef()
    {
        ref int result = ref _dict.GetValueRefOrNullRef("not-found");
        Assert.That(Unsafe.IsNullRef(ref result));
    }

    [Test]
    public void Remove_ExistingKey_RemovesKey()
    {
        ref int valueRef = ref _dict.GetValueRefOrAddDefault("to-remove", out bool exists);
        valueRef = 7;

        bool removed = _dict.Remove("to-remove");
        Assert.That(removed);

        ref int result = ref _dict.GetValueRefOrNullRef("to-remove");
        Assert.That(Unsafe.IsNullRef(ref result));
    }

    [Test]
    public void Remove_ExistingKey_DoesNotEnumerateRemovedEntry()
    {
        _dict.GetValueRefOrAddDefault("keep-1", out _) = 1;
        _dict.GetValueRefOrAddDefault("remove", out _) = 2;
        _dict.GetValueRefOrAddDefault("keep-2", out _) = 3;

        _dict.Remove("remove");

        List<string> keys = [];
        foreach (var entry in _dict)
            keys.Add(entry.Key);

        Assert.That(keys, Is.EquivalentTo(new[] { "keep-1", "keep-2" }));
    }

    [Test]
    public void GetValueRefOrAddDefault_ReusedSlot_ReturnsDefaultValue()
    {
        _dict.GetValueRefOrAddDefault("old", out _) = 42;
        _dict.Remove("old");

        ref int value = ref _dict.GetValueRefOrAddDefault("new", out bool exists);

        Assert.That(exists, Is.False);
        Assert.That(value, Is.Zero);
    }

    [Test]
    public void AddMultipleEntries_Resize_AndRetrieveAll()
    {
        for (int i = 0; i < 20; i++)
        {
            ref int valueRef = ref _dict.GetValueRefOrAddDefault("key" + i, out bool exists);
            valueRef = i * 10;
        }

        for (int i = 0; i < 20; i++)
        {
            ref int found = ref _dict.GetValueRefOrNullRef("key" + i);
            Assert.IsFalse(Unsafe.IsNullRef(ref found));
            Assert.That(found, Is.EqualTo(i * 10));
        }
    }

    [Test]
    public void UpdateValueViaRef_ReflectsInDictionary()
    {
        ref int valueRef = ref _dict.GetValueRefOrAddDefault("counter", out bool exists);
        valueRef = 1;

        valueRef++;
        ref int result = ref _dict.GetValueRefOrNullRef("counter");
        Assert.That(result, Is.EqualTo(2));
    }
}
