using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Frent.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class UsageAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor DuplicateCreateTypeParameter = new(
        id: "FR0004",
        title: "Duplicate Component Type",
        messageFormat: "World.Create() called with duplicate component type parameter {0}",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
        ImmutableArray.Create(
            DuplicateCreateTypeParameter);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax typeDeclarationSyntax = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(typeDeclarationSyntax).Symbol is not IMethodSymbol m)
            return;
        
        if (m is not 
            {
                IsGenericMethod: true,
                Name: "Create", 
                ContainingType:
                {
                    Name: "World",
                    ContainingNamespace:
                    {
                        Name: "Frent",
                        ContainingNamespace.IsGlobalNamespace: true
                    } 
                } 
            })
            return;

        HashSet<ITypeSymbol> symbols = new(SymbolEqualityComparer.Default);

        foreach (var typeArg in m.TypeArguments)
        {
            if (!symbols.Add(typeArg))
            {
                Report(DuplicateCreateTypeParameter, m, typeArg.ToDisplayString(), typeArg.Name);
            }
        }

        void Report(DiagnosticDescriptor diagnosticDescriptor, ISymbol location, params object?[] args)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location.Locations.First(), args));
        }
    }
}