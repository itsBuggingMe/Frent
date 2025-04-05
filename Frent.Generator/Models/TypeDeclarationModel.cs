using Microsoft.CodeAnalysis;
namespace Frent.Generator.Models;
internal record struct TypeDeclarationModel(bool IsRecord, TypeKind TypeKind, string Name);