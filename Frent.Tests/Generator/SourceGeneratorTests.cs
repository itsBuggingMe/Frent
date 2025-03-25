using Frent.Core;
using Frent.Tests.Helpers;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Generator;

internal class SourceGeneratorTests
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

    //TODO: source generator refactor
    // [x] code builder
    // [ ] merge generators
    // [ ] write tests
}
