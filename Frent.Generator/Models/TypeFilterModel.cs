using Frent.Variadic.Generator;

namespace Frent.Generator.Models;

internal record struct TypeFilterModel(EquatableArray<string> Allow, EquatableArray<string> Disallow);