using Frent.Variadic.Generator;
using Microsoft.CodeAnalysis;

namespace Frent.Generator.Model;

internal record struct SourceOutput(string? Name, string Source, EquatableArray<Diagnostic>? Diagnostics);