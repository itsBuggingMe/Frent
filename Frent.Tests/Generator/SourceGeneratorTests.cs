using Frent.Components;
using Frent;
using static NUnit.Framework.Assert;


namespace Frent.Tests.Generator
{
    internal partial class SourceGeneratorTests
    {
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

        private static void TestTypeRegistration<T>(TypeRegistrationFlags typeFlags)
            where T : new()
        {
            using World world = new(new DefaultUniformProvider()
                .Add<float>(0));

            Entity test = world.Create();
            if (typeFlags.HasFlag(TypeRegistrationFlags.Initable))
                Throws<InitalizeException>(() => test.Add(new T()));
            else
                test.Add(new T());

            if (typeFlags.HasFlag(TypeRegistrationFlags.Updateable))
                Throws<UpdateException>(world.Update);
            else
                world.Update();

            if (typeFlags.HasFlag(TypeRegistrationFlags.Destroyable))
                Throws<DestroyException>(test.Remove<T>);
            else
                test.Remove<T>();
        }

        [Flags]
        internal enum TypeRegistrationFlags
        {
            Initable = 1 << 0,
            Destroyable = 1 << 1,
            Updateable = 1 << 2,
        }

        public partial class Nest<T>
        {
            internal partial struct Inner<T1> : IInitable
            {
                public void Init(Entity self)
                {
                    throw new InitalizeException();
                }
            }

            private partial class InnerPartially<T1> : IComponent
            {
                public void Update()
                {
                    throw new UpdateException();
                }
            }
        }

        private partial struct IndirectInterface : ILifetimeInterface
        {
            public void Destroy()
            {
                throw new DestroyException();
            }

            public void Init(Entity self)
            {
                throw new InitalizeException();
            }

            public void Update()
            {
                throw new UpdateException();
            }
        }
    }
}

// Global Namespace

internal partial class InGlobalNamespace : IComponent
{
    public void Update()
    {
        throw new UpdateException();
    }

    internal partial struct Inner<T> : IComponentBase
    {

        internal partial struct Unbound<T1> : IUniformComponent<T1>
        {
            public void Update(T1 uniform)
            {
                throw new UpdateException();
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
            throw new InitalizeException();
        }
    }

    //this should produce a warning
    protected partial class Warning : IComponent, IUniformComponent<int>
    {
        public void Update()
        {
            throw new UpdateException();
        }

        public void Update(int uniform)
        {
            throw new UpdateException();
        }
    }
}
internal interface ILifetimeInterface : IComponent, IInitable, IDestroyable
{

}


internal class InitalizeException : Exception;

internal class DestroyException : Exception;

internal class UpdateException : Exception;