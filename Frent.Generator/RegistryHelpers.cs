using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frent.Generator;

public static class RegistryHelpers
{
    private static SymbolDisplayFormat? _symbolDisplayFormat;
    public static SymbolDisplayFormat FullyQualifiedTypeNameFormat => _symbolDisplayFormat ??= new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

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

    public static bool IsTag(this INamedTypeSymbol symbol) => symbol is
    {
        Name: TagInterfaceName,
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

    public static bool IsEntity(this ITypeSymbol? symbol) => symbol is
    {
        Name: "Entity",
        ContainingNamespace:
        {
            Name: "Frent",
        }
    };

    public static bool IsSpecialComponentInterface(this INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol is
    {
        Name: TargetInterfaceName or InitableInterfaceName or DestroyableInterfaceName or SparseInterfaceName,
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

    public static bool IsSerializeComponentInterface(this INamedTypeSymbol type) => type is
    {
        Name: IOnSerializeInterfaceName or IOnDeserializeInterfaceName,
        ContainingNamespace:
        {
            Name: "Serialization",
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

    public static bool IsUniformComponentInterface(this INamedTypeSymbol type) => type is
    {
        Name: UniformComponentInterfaceName or EntityUniformComponentInterfaceName,
    };

    public const string IncludesComponentsAttributeName = "Frent.Updating.IncludesComponentsAttribute";
    public const string ExcludesComponentsAttributeName = "Frent.Updating.ExcludesComponentsAttribute";
    public const string IncludesTagsAttributeName = "Frent.Updating.IncludesTagsAttribute";
    public const string ExcludesTagsAttributeName = "Frent.Updating.ExcludesTagsAttribute";

    public const string UpdateTypeAttributeName = "Frent.Updating.UpdateTypeAttribute";

    public const string UpdateMethodName = "Update";
    public const string FileName = "ComponentUpdateTypeRegistry.g.cs";

    public const string FullyQualifiedTargetInterfaceName = "Frent.Components.IComponentBase";
    public const string FullyQualifiedInitableInterfaceName = "Frent.Components.IInitable";
    public const string FullyQualifiedDestroyableInterfaceName = "Frent.Components.IDestroyable";
    public const string FullyQualifiedSparseInterfaceName = "Frent.Components.ISparseComponent";
    public const string FullyQualifiedTagInterfaceName = "Frent.Components.ITag";

    public const string FullyQualifiedIOnSerializeInterfaceName = "Frent.Serialization.IOnSerialize";
    public const string FullyQualifiedIOnDeserializeInterfaceName = "Frent.Serialization.IOnDeserialize";

    public const string TargetInterfaceName = "IComponentBase";
    public const string InitableInterfaceName = "IInitable";
    public const string DestroyableInterfaceName = "IDestroyable";
    public const string SparseInterfaceName = "ISparseComponent";
    public const string TagInterfaceName = "ITag";

    public const string IOnSerializeInterfaceName = "IOnSerialize";
    public const string IOnDeserializeInterfaceName = "IOnDeserialize";

    public const string UniformComponentInterfaceName = "IUniformComponent";
    public const string EntityUniformComponentInterfaceName = "IEntityUniformComponent";

    public const string FrentComponentNamespace = "Frent.Components";

    public const string ComponentJsonSerializerContextAttributeMetadataName = "Frent.Serialization.ComponentJsonSerializerContextAttribute";
}

