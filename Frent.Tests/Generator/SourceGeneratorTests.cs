using System.Diagnostics;
using System.Runtime.CompilerServices;
using Frent;
using Frent.Components;
using static Frent.Tests.Generator.SourceGeneratorTests;
using static NUnit.Framework.Assert;


namespace Frent.Tests.Generator
{
    internal partial class SourceGeneratorTests
    {
        public static TypeRegistrationFlags EventFlags;

        //TODO: Cases to test for
        //N deep class/struct nesting
        //public/private/internal
        //generic container
        //global namespace
        //hidden inheritance
        //init/destroy/component
        //multiple component interfaces
        //abstract classes, interfaces implementing interfaces
        //conflicting name aliases
        //ref structs should error
        //generic components
        //unbound generic IComponent<T> interfaces
        //unity versions, InitalizeOnLoadMethod vs RuntimeInitalizeOnLoadMethod
        //conflicting generic type names

        //TODO: source generator refactor
        // [x] code builder
        // [x] merge generators
        // [x] write tests

        [Test]
        public void RegisteredProperly_Inner() =>
            TestTypeRegistration<Nest<int>.Inner<float>>(TypeRegistrationFlags.Initable);

        [Test]
        public void RegisteredProperly_IndirectInterface() =>
            TestTypeRegistration<IndirectInterface>(TypeRegistrationFlags.Initable | TypeRegistrationFlags.Destroyable | TypeRegistrationFlags.Updateable);

        [Test]
        public void RegisteredProperly_InGlobalNamespace() =>
            TestTypeRegistration<InGlobalNamespace>(TypeRegistrationFlags.Updateable);

        [Test]
        public void RegisteredProperly_InGlobalNamespaceInner() =>
            TestTypeRegistration<InGlobalNamespace.Inner<object>>(default);

        [Test]
        public void RegisteredProperly_InGlobalNamespaceInnerUnbound() =>
            TestTypeRegistration<InGlobalNamespace.Inner<object>.Unbound<float>>(TypeRegistrationFlags.Updateable);

        [Test]
        public void RegisteredProperly_Derived() =>
            TestTypeRegistration<Derived>(TypeRegistrationFlags.Updateable);

        [Test]
        public void RegisteredProperly_DerivedInner() =>
            TestTypeRegistration<Derived.DerivedInner>(TypeRegistrationFlags.Initable | TypeRegistrationFlags.Updateable);

        [Test]
        public void ValueTuple_AutomaticallyCreated()
        {
            using World world = new(new DefaultUniformProvider()
                .Add<float>(42)
                .Add<int>(67));

            world.Create(new ValueTupleUniform(i =>
            {
                That(i, Is.EqualTo((42, 67)));
            }));

            world.Update();
        }

        private static void TestTypeRegistration<T>(TypeRegistrationFlags typeFlags)
            where T : new()
        {
            using World world = new(new DefaultUniformProvider()
                .Add<float>(0));

            EventFlags = default;

            Entity test = world.Create();
            test.Add(new T());
            world.Update();
            test.Remove<T>();

            That(typeFlags, Is.EqualTo(EventFlags));
        }

        [Flags]
        internal enum TypeRegistrationFlags
        {
            Initable = 1 << 0,
            Destroyable = 1 << 1,
            Updateable = 1 << 2,
        }

        internal class ValueTupleUniform(Action<(float, int)> onUpdate) : 
            IUniformUpdate<(float, int)>,
            IEntityUniformUpdate<(float, int)>,
            IEntityUniformUpdate<(float, int), ValueTupleUniform>
        {
            public void Update((float, int) uniform)
            {
                onUpdate(uniform);
            }

            public void Update(Entity self, (float, int) uniform, ref ValueTupleUniform arg)
            {
                onUpdate(uniform);
            }

            public void Update(Entity self, (float, int) uniform)
            {
                onUpdate(uniform);
            }
        }

        public partial class Nest<T>
        {
            internal partial struct Inner<T1> : IInitable
            {
                public void Init(Entity self)
                {
                    EventFlags |= TypeRegistrationFlags.Initable;
                }
            }

            private partial class InnerPartially<T1> : IUpdate
            {
                public void Update()
                {
                    EventFlags |= TypeRegistrationFlags.Updateable;
                }
            }
        }

        private partial struct IndirectInterface : ILifetimeInterface
        {
            public void Destroy()
            {
                EventFlags |= TypeRegistrationFlags.Destroyable;
            }

            public void Init(Entity self)
            {
                EventFlags |= TypeRegistrationFlags.Initable;
            }

            public void Update()
            {
                EventFlags |= TypeRegistrationFlags.Updateable;
            }
        }
    }
}

// Global Namespace

internal partial class InGlobalNamespace : IUpdate
{
    public void Update()
    {
        EventFlags |= TypeRegistrationFlags.Updateable;
    }

    internal partial struct Inner<T> : IComponentBase
    {

        internal partial struct Unbound<T1> : IUniformUpdate<T1>
        {
            public void Update(T1 uniform)
            {
                EventFlags |= TypeRegistrationFlags.Updateable;
            }
        }
    }
}

internal partial class Derived : InGlobalNamespace
{
    internal partial class DerivedInner : Derived, IInitable
    {
        public void Init(Entity self)
        {
            EventFlags |= TypeRegistrationFlags.Initable;
        }
    }

    //this should produce a warning
    protected partial class Warning : IUpdate, IUniformUpdate<int>
    {
        public void Update()
        {
            EventFlags |= TypeRegistrationFlags.Updateable;
        }

        public void Update(int uniform)
        {
            EventFlags |= TypeRegistrationFlags.Updateable;
        }
    }
}
internal interface ILifetimeInterface : IUpdate, IInitable, IDestroyable
{

}


internal class InitalizeException : Exception;

internal class DestroyException : Exception;

internal class UpdateException : Exception;