using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frent.Generator;

public static class RegistryHelpers
{
    public static StringBuilder AppendNamespace(this StringBuilder sb, string @namespace)
    {
        if (@namespace == string.Empty)
            return sb;
        return sb.Append(@namespace).Append('.');
    }

    public static StringBuilder AppendFullTypeName(this StringBuilder sb, string typeName)
    {
        return sb.Append("global::").Append(typeName);
    }

    public static bool IsPartial(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.DeclaringSyntaxReferences
            .Select(syntaxRef => syntaxRef.GetSyntax() as TypeDeclarationSyntax)
            .Any(syntax => syntax?.Modifiers.Any(SyntaxKind.PartialKeyword) ?? false);
    }

    public static bool IsOrExtendsIComponentBase(this INamedTypeSymbol symbol)
    {
        if (symbol.IsIComponentBase())
            return true;
        foreach(var @interface in symbol.Interfaces)
        {
            if(@interface.IsIComponentBase())
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsIComponentBase(this INamedTypeSymbol symbol) => symbol is
    {
        Name: TargetInterfaceName,
        ContainingNamespace:
        {
            Name: "Components",
            ContainingNamespace:
            {
                Name: "Frent",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    public static bool IsSpecialComponentInterface(this INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol is
    {
        Name: TargetInterfaceName or InitableInterfaceName or DestroyableInterfaceName,
        ContainingNamespace:
        {
            Name: "Components",
            ContainingNamespace:
            {
                Name: "Frent",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    public static bool IsFrentComponentInterface(this INamedTypeSymbol type) => type is
    {
        ContainingNamespace:
        {
            Name: "Components",
            ContainingNamespace:
            {
                Name: "Frent",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    public const string UpdateTypeAttributeName = "Frent.Updating.UpdateTypeAttribute";
    public const string UpdateOrderInterfaceName = "Frent.Updating.IComponentUpdateOrderAttribute";
    public const string UpdateMethodName = "Update";
    public const string FileName = "ComponentUpdateTypeRegistry.g.cs";
    public const string FullyQualifiedTargetInterfaceName = "Frent.Components.IComponentBase";
    public const string FullyQualifiedInitableInterfaceName = "Frent.Components.IInitable";
    public const string FullyQualifiedDestroyableInterfaceName = "Frent.Components.IDestroyable";

    public const string TargetInterfaceName = "IComponentBase";
    public const string InitableInterfaceName = "IInitable";
    public const string DestroyableInterfaceName = "IDestroyable";

    public const string FrentComponentNamespace = "Frent.Components";
}

