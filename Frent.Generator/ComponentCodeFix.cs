using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Frent.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ComponentCodeFix : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

#pragma warning disable RS2008 // Enable analyzer release tracking
    private static readonly DiagnosticDescriptor Diagnostic = new DiagnosticDescriptor(
        id: "FR0005",
        title: "Rename IComponent to IUpdate",
        messageFormat: "",
        category: "Breaking Changes",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(Diagnostic);


    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        const string LegacyName = "IComponent";
        const string LegacyNameEnd = "Component";

        IdentifierNameSyntax identifier = (IdentifierNameSyntax)context.Node;

        TextSpan identifierSpan = identifier.Span;
        if (identifierSpan.Length < LegacyName.Length)
            return;
        if (!context.Node.SyntaxTree.TryGetText(out SourceText? text))
            return;
        if (text[identifierSpan.Start] != LegacyName[0])
            return;

        int endIndex = identifierSpan.End - 1;
        for(int i = 0; i < LegacyNameEnd.Length; i++)
        {
            if (text[endIndex - i] != LegacyNameEnd[LegacyNameEnd.Length - i - 1])
                return;
        }

        string identifierText = text.ToString(identifierSpan);
        if (identifierText == RegistryHelpers.SparseInterfaceName)
            return;

        var info = context.SemanticModel.GetSymbolInfo(identifier);

        ComponentUpdateTypeRegistryGenerator.LaunchDebugger();

        GC.KeepAlive(info);
        GC.KeepAlive(identifierText);
    }
}
