using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Diagnostics;

namespace Frent.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class GeneratorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;
    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

    static GeneratorAnalyzer()
    {
        var b = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(3);
        b.Add(NonPartialGenericComponent);
        b.Add(NonPartialOuterInaccessibleType);
        b.Add(NonPartialNestedInaccessibleType);
        _supportedDiagnostics = b.MoveToImmutable();
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration);
    }
    
    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        TypeDeclarationSyntax typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol namedTypeSymbol)
            return;

        bool isComponent = false;
        //int updateInterfaceCount = 0;


        foreach(var @interface in namedTypeSymbol.AllInterfaces)
        {
            if (!@interface.IsOrExtendsIComponentBase())
                return;

            isComponent = true;

            break;
            //if(!@interface.IsSpecialComponentInterface() && @interface.IsFrentComponentInterface())
            //{//if its a Frent interface and is not Initable, Destroyable, or IComponentBase, it must be one of the update interfaces
            //    updateInterfaceCount++;
            //}
        }

        if (!isComponent)
            return;

        bool isPartial = namedTypeSymbol.IsPartial();
        bool componentTypeIsAcsessableFromModule =
            namedTypeSymbol.DeclaredAccessibility == Accessibility.Public ||
            namedTypeSymbol.DeclaredAccessibility == Accessibility.Internal;

        if (!isPartial)
        {
            if(namedTypeSymbol.IsGenericType)
            {
                Report(NonPartialGenericComponent, namedTypeSymbol, namedTypeSymbol.Name);
            }
            else if(!componentTypeIsAcsessableFromModule)
            {
                Report(NonPartialNestedInaccessibleType, namedTypeSymbol, namedTypeSymbol.Name);
            }
        }

        INamedTypeSymbol current = namedTypeSymbol;
        while (current.ContainingType is not null)
        {
            current = current.ContainingType;

            if (!componentTypeIsAcsessableFromModule && !current.IsPartial())
                Report(NonPartialOuterInaccessibleType, current, current.Name);
        }

        //if(updateInterfaceCount > 1)
        //{
        //    Report(MultipleComponentInterfaces, current);
        //}

        void Report(DiagnosticDescriptor diagnosticDescriptor, ISymbol location, params object?[] args)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location.Locations.First(), args));
        }
    }

#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor NonPartialGenericComponent = new(
        id: "FR0000",
        title: "Non-partial Generic Component Type",
        messageFormat: "Generic Component '{0}' must be marked as partial",
        category: "Source Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonPartialOuterInaccessibleType = new(
        id: "FR0001",
        title: "Non-partial Outer Inaccessible Type",
        messageFormat: "Outer type of inaccessible nested component type '{0}' must be marked as partial",
        category: "Source Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonPartialNestedInaccessibleType = new(
        id: "FR0002",
        title: "Non-partial Nested Inaccessible Component Type",
        messageFormat: "Inaccessible Nested Component Type '{0}' must be marked as partial",
        category: "Source Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
