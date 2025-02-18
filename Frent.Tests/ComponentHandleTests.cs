using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class ComponentHandleTests
{
    [Test]
    public void ComponentHandle_StoresComponent()
    {
        using var handle = ComponentHandle.Create(69);
        That(handle.Retrieve<int>(), Is.EqualTo(69));
    }

    [Test]
    public void Retrieve_ThrowsWrongType()
    {
        using var handle = ComponentHandle.Create(69);
        Throws<InvalidOperationException>(() => handle.Retrieve<double>());
    }

    [Test]
    public void RetrieveBoxed_CorrectValue()
    {
        using var handle = ComponentHandle.Create(69);
        object box = handle.RetrieveBoxed();
        That(box.GetType(), Is.EqualTo(typeof(int)));
        That((int)box, Is.EqualTo(69));
    }

    [Test]
    public void Type_CorrectType()
    {
        ComponentHandle[] handle = 
        [
            ComponentHandle.Create(0),
            ComponentHandle.Create(0.0),
            ComponentHandle.Create(0f)
        ];

        That(handle.Select(c => c.Type), Is.EqualTo((Type[])[typeof(int), typeof(double), typeof(float)]));
        That(handle.Select(c => c.ComponentID), Is.EqualTo((ComponentID[])[Component<int>.ID, Component<double>.ID, Component<float>.ID]));
    }

    [Test]
    public void ComponentHandle_Disposes()
    {
        ComponentHandle.Create<float>(0).Dispose();

        Memory.Record();
        for(int i = 0; i < 10_000; i++)
        {
            ComponentHandle.Create<float>(0).Dispose();
        }
        Memory.NotAllocated();
    }
}
