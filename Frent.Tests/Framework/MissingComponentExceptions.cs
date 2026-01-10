using Frent.Components;
using Frent.Tests.Helpers;
using Frent.Updating;
using Frent.Core;
using static Frent.Tests.SparseComponents.SparseComponentVariadicTests;
using static NUnit.Framework.Assert;
using System.Diagnostics;

namespace Frent.Tests.Framework;

internal class MissingComponentExceptions
{
    /*
    *               | sparse   | archetype
    * update        | [ ] [ ]  | [x] [ ]
    * single comp   | [ ] [ ]  | [ ] [ ]
    * multithreaded | [ ] [ ]  | [ ] [ ]
    * 
    *                 normal vs deferred
    */

    [Test]
    public void MissingDependency_Sparse(
        [Values(false)] bool deferred,
        [Values(UpdateKind.Normal, UpdateKind.Component)] UpdateKind kind)
    {
        TestMissingComponentException<SparseComponentWithDependency>(deferred, kind);
    }

    [Test]
    public void MissingDependency_Archetype(
        [Values(false)] bool deferred,
        [Values(UpdateKind.Normal, UpdateKind.Component)] UpdateKind kind)
    {
        TestMissingComponentException<ComponentWithDependency>(deferred, kind);
    }


    private void TestMissingComponentException<TComponent>(bool deferred, UpdateKind kind) where TComponent : struct
    {
        using World world = new World()
        {
            UpdateDeferredCreationEntities = true
        };

        for (int i = 0; i < 100; i++)
            world.Create(default(TComponent), default(Dependency));

        Entity failed = default;
        if(deferred)
            world.Create(new DelegateBehavior(() => failed = world.Create(default(TComponent))));
        else
            failed = world.Create(default(TComponent));

        for (int i = 0; i < 100; i++)
            world.Create(default(TComponent), default(Dependency));

        MissingComponentException? exception = Throws<MissingComponentException>(kind switch
        {
            UpdateKind.Normal => world.Update,
            UpdateKind.Component => () => world.UpdateComponent(Component<TComponent>.ID),
            UpdateKind.Multithread => world.Update<MultithreadedUpdate>,
            _ => throw new UnreachableException(),
        });

        That(exception, Is.Not.Null);
        That(exception!.InvalidEntity, Is.EqualTo(failed));
        That(exception.MissingComponent, Is.EqualTo(typeof(Dependency)));
        That(exception.ComponentType, Is.EqualTo(typeof(TComponent)));
    }

    internal enum UpdateKind
    {
        Normal,
        Component,
        Multithread,
    }

    internal struct Dependency;

    internal struct SparseComponentWithDependency : IUpdate<Dependency>, ISparseComponent
    {
        [MultithreadedUpdate]
        public void Update(ref Dependency arg) { }
    }

    internal struct ComponentWithDependency : IUpdate<Dependency>
    {
        [MultithreadedUpdate]
        public void Update(ref Dependency arg) { }
    }

    internal class MultithreadedUpdate : MultithreadUpdateTypeAttribute;
}