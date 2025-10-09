using Frent.Variadic.Generator;

namespace Frent.Generator.Models;

internal record struct UpdateMethodModel(EquatableArray<string> Attributes, EquatableArray<string> GenericArguments, EquatableArray<string> UniformTupleTypes, string ImplInterface);