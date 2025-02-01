using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class DefaultUniformProviderTests
{
    [Test]
    public void Add_AddsUniform()
    {
        var uniformProvider = new DefaultUniformProvider();
        var instance = new Class1();

        uniformProvider.Add(typeof(Class1), instance);
        That(uniformProvider.GetUniform<Class1>(), Is.EqualTo(instance));
    }

    [Test]
    public void AddGeneric_AddsUniform()
    {
        var uniformProvider = new DefaultUniformProvider();
        var instance = new Class1();

        uniformProvider.Add(instance);
        That(uniformProvider.GetUniform<Class1>(), Is.EqualTo(instance));
    }

    [Test]
    public void Add_ThrowsNotAssigned()
    {
        var uniformProvider = new DefaultUniformProvider();
        Throws<ArgumentException>(() => uniformProvider.Add(typeof(Class1), new Class2()));
    }
}