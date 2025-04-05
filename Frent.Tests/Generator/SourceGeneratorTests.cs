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
        // [ ] write tests



        public partial class Nest<T>
        {
            private partial struct Inner<T1> : IInitable
            {
                public void Init(Entity self)
                {
                    throw new NotImplementedException();
                }
            }

            private partial class InnerPartially<T1> : IComponent
            {
                public void Update()
                {
                    throw new NotImplementedException();
                }
            }
        }

        private partial struct IndirectInterface : ILifetimeInterface
        {
            public void Destroy()
            {
                throw new NotImplementedException();
            }

            public void Init(Entity self)
            {
                throw new NotImplementedException();
            }

            public void Update()
            {
                throw new NotImplementedException();
            }
        }
    }
}

// Global Namespace

internal partial class InGlobalNamespace : IComponent
{
    public void Update()
    {
        throw new NotImplementedException();
    }

    private partial struct Inner<T> : IComponentBase
    {

        internal partial struct Unbound<T1> : IUniformComponent<T1>
        {
            public void Update(T1 uniform)
            {
                throw new NotImplementedException();
            }
        }
    }
}

internal partial class Derived : InGlobalNamespace
{
    private partial class DerivedInner : Derived, IInitable
    {
        public void Init(Entity self)
        {
            throw new NotImplementedException();
        }
    }

    //this should produce a warning
    protected partial class Warning : IComponent, IUniformComponent<int>
    {
        public void Update()
        {
            throw new NotImplementedException();
        }

        public void Update(int uniform)
        {
            throw new NotImplementedException();
        }
    }
}
internal interface ILifetimeInterface : IComponent, IInitable, IDestroyable
{

}