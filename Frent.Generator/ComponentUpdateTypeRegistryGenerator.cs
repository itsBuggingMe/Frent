using Frent.Variadic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Frent.Generator;

[Generator(LanguageNames.CSharp)]//TODO: refactor into CodeBuilder
public class ComponentUpdateTypeRegistryGenerator : IIncrementalGenerator
{
    private static SymbolDisplayFormat? _symbolDisplayFormat;
    private static SymbolDisplayFormat FullyQualifiedTypeNameFormat => _symbolDisplayFormat ??= new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(static (n, _) => n is TypeDeclarationSyntax typeDec && typeDec.BaseList is not null,
            static (gsc, ct) =>
            {
                INamedTypeSymbol? componentTypeSymbol = gsc.SemanticModel.GetDeclaredSymbol(gsc.Node, ct) as INamedTypeSymbol;

                if (componentTypeSymbol is not null && 
                    (componentTypeSymbol.TypeKind == TypeKind.Class || componentTypeSymbol.TypeKind == TypeKind.Struct))
                {
                    foreach (var @interface in componentTypeSymbol.AllInterfaces)
                    {
                        if (InterfaceImplementsIComponent(@interface))
                        {
                            string @namespace = componentTypeSymbol.ContainingNamespace.ToString();
                            if (@namespace == "<global namespace>")
                                @namespace = string.Empty;
                            int index = @namespace.IndexOf('.');
                            var genericArgs = @interface.TypeArguments.Length == 0 ? [] : new string[@interface.TypeArguments.Length];

                            for (int i = 0; i < @interface.TypeArguments.Length; i++)
                            {
                                ITypeSymbol namedTypeSymbol = @interface.TypeArguments[i];
                                genericArgs[i] = namedTypeSymbol.ToDisplayString(FullyQualifiedTypeNameFormat);
                            }

                            //stack allocate 6 slots
                            var stackAttributes = new StackStack<string>([null!, null!, null!, null!, null!, null!]);

                            UpdateModelFlags flags = default;
                            if (ImplementsInterface(componentTypeSymbol, RegistryHelpers.FullyQualifiedInitableInterfaceName))
                                flags |= UpdateModelFlags.Initable;
                            if (ImplementsInterface(componentTypeSymbol, RegistryHelpers.FullyQualifiedDestroyableInterfaceName))
                                flags |= UpdateModelFlags.Destroyable;

                            if (componentTypeSymbol.IsGenericType)
                                flags |= UpdateModelFlags.IsGeneric;

                            if (componentTypeSymbol.TypeKind == TypeKind.Class)
                                flags |= UpdateModelFlags.IsClass;
                            if (componentTypeSymbol.TypeKind == TypeKind.Struct)
                                flags |= UpdateModelFlags.IsStruct;

                            if(componentTypeSymbol.IsRecord)
                                flags |= UpdateModelFlags.IsRecord;

                            Diagnostic? diagnostic = null;

                            if(componentTypeSymbol.IsGenericType && !IsPartial(componentTypeSymbol))
                            {
                                diagnostic = Diagnostic.Create(
                                        new DiagnosticDescriptor(
                                            id: "FR0000",
                                            title: "Non-partial Generic Component Type",
                                            messageFormat: "Generic Component \'{0}\' must be marked as partial.",
                                            category: "Source Generation",
                                            DiagnosticSeverity.Error,
                                            isEnabledByDefault: true
                                            ), 
                                        componentTypeSymbol.Locations.First(), 
                                        componentTypeSymbol.Name);
                            }

                            foreach (var item in ((TypeDeclarationSyntax)gsc.Node).Members)
                            {
                                if (item is MethodDeclarationSyntax method && method.AttributeLists.Count != 0 && method.Identifier.ToString() == RegistryHelpers.UpdateMethodName)
                                {
                                    foreach (var attrList in method.AttributeLists)
                                    {
                                        foreach (var attr in attrList.Attributes)
                                        {
                                            if (gsc.SemanticModel.GetSymbolInfo(attr).Symbol is IMethodSymbol attrCtor)
                                            {
                                                if(InheritsFromBase(attrCtor.ContainingType, RegistryHelpers.UpdateTypeAttributeName))
                                                {
                                                    stackAttributes.Push(attrCtor.ContainingType.ToString());
                                                }

                                                //if(ImplementsInterface(attrCtor.ContainingType, RegistryHelpers.UpdateOrderInterfaceName) && attrCtor.Parameters.Length > 0)
                                                //{
                                                //    if(attrCtor.Parameters[0].ExplicitDefaultValue is int updateorder)
                                                //    {
                                                //        order = updateorder;
                                                //    }
                                                //}
                                            }
                                        }
                                    }
                                }
                            }

                            //TODO: avoid alloc?
                            return new ComponentUpdateItemModel(
                                flags,
                                componentTypeSymbol.ToString(),
                                componentTypeSymbol.Name,
                                @interface.Name,
                                index == -1 ? @namespace : @namespace.Substring(0, index),
                                index == -1 ? string.Empty : @namespace.Substring(index + 1),
                                new EquatableArray<string>(genericArgs),
                                new EquatableArray<string>(stackAttributes.ToArray()),
                                diagnostic);
                        }
                    }
                }

                return new ComponentUpdateItemModel(default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new([]), new([]), null);
            });

        IncrementalValuesProvider<ComponentUpdateItemModel> types = models
            .Where(m => m.Type.Length != 0);

#if UNITY
        //unity generation
        IncrementalValueProvider<SourceOutput> monolith = types
            .Where(m => !m.Flagged(UpdateModelFlags.IsGeneric))
            .Collect()
            .Select(GenerateMonolithicRegistrationFile);

        context.RegisterImplementationSourceOutput(monolith, (ctx, s) =>
        {
            if (s.Diagnostic is not null)
                ctx.ReportDiagnostic(s.Diagnostic);
            if (s.Name is not null)
                ctx.AddSource(s.Name, s.Source);
        });

        var partialDeclarations = types
            .Where(m => m.Flagged(UpdateModelFlags.IsGeneric))
            .Select((n, ct) => GenerateRegisterGenericType(n, ct));

        context.RegisterImplementationSourceOutput(partialDeclarations, (ctx, s) =>
        {
            if (s.Diagnostic is not null)
                ctx.ReportDiagnostic(s.Diagnostic);
            if (s.Name is not null)
                ctx.AddSource(s.Name, s.Source);
        });
#else
        //normal generation
        IncrementalValuesProvider<SourceOutput> files = types
            .Select((t, ct) => t.Flagged(UpdateModelFlags.IsGeneric) ? GenerateRegisterGenericType(t, ct) : GenerateModuleInitalizerFiles(t, ct));

        context.RegisterImplementationSourceOutput(files, (ctx, s) =>
        {
            if(s.Diagnostic is not null)
                ctx.ReportDiagnostic(s.Diagnostic);
            if (s.Name is not null)
                ctx.AddSource(s.Name, s.Source);
        });
#endif
    }

    private static SourceOutput GenerateMonolithicRegistrationFile(ImmutableArray<ComponentUpdateItemModel> models, CancellationToken ct)
    {
        if (models.Length == 0)
            return new(default, string.Empty, default);

        StringBuilder sb = new StringBuilder();


        sb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using global::Frent.Updating;")
            .AppendLine("using global::Frent.Updating.Runners;")
            .AppendLine("using global::System.Runtime.CompilerServices;")
            .AppendLine();

        sb
            .AppendLine()
            .AppendLine("public static class FrentComponentRegistry")
            .AppendLine("{")
                .AppendLine("    [UnityEngine.RuntimeInitializeOnLoadMethod]")
                .AppendLine("    public static void RegisterAll()")
                .AppendLine("    {");

        foreach(ref readonly var model in models.AsSpan())
        {
            AppendInitalizationMethodBody(sb, in model);
            ct.ThrowIfCancellationRequested();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        string source = sb.ToString();
        sb.Clear();

        return new("FrentComponentRegistry.g.cs", source, null);
    }

    private static bool AreModuleInitalizersSupported(ParseOptions options)
    {
        foreach(var e in options.PreprocessorSymbolNames)
        {
            if (e == "NET6_0_OR_GREATER")
            {
                return true;
            }
        }
        return false;
    }

    private static SourceOutput GenerateModuleInitalizerFiles(in ComponentUpdateItemModel model, CancellationToken ct)
    {
        Debug.Assert(!model.Flagged(UpdateModelFlags.IsGeneric));

        StringBuilder sb = new StringBuilder();

        sb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using global::Frent.Updating;")
            .AppendLine("using global::Frent.Updating.Runners;")
            .AppendLine();

        if (model.BaseNamespace != string.Empty)
            sb
                .Append("namespace ").Append(model.BaseNamespace).Append(';').AppendLine();

        sb
            .AppendLine()
            .AppendLine("[global::System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
            .Append("internal static partial class ").Append(model.Type).AppendLine("ComponentUpdateInitalizer_")
            .AppendLine("{")
                .AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]")
                .AppendLine("    [global::System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
                .Append("    internal static void Initalize").Append(model.FullName.Replace('.', '_')).AppendLine("()")
                .AppendLine("    {");

        ct.ThrowIfCancellationRequested();

        AppendInitalizationMethodBody(sb, in model);

        ct.ThrowIfCancellationRequested();

        //end method ^& class
        sb.AppendLine("    }");
        sb.AppendLine("}");

        string source = sb.ToString();
        sb.Clear();

        sb.AppendNamespace(model.SubNamespace).Append(model.Type).Append("ComponentUpdateInitalizer").Append(".g.cs");

        string name = sb.ToString();
        sb.Clear();

        return new(name, source, model.Diagnostic);
    }

    private static void AppendInitalizationMethodBody(StringBuilder sb, in ComponentUpdateItemModel model)
    {
        var span = ExtractUpdaterName(model.ImplInterface);
        
        sb
            .Append("        GenerationServices.RegisterType(typeof(")
            .AppendFullTypeName(model.FullName)
            .Append("), new ");

        (model.ImplInterface == RegistryHelpers.TargetInterfaceName ? sb.Append("None") : sb.Append(model.ImplInterface, span.Start, span.Count))
            .Append("UpdateRunnerFactory")
            .Append('<')
            .AppendFullTypeName(model.FullName);

        foreach (var item in model.GenericArguments)
            sb.Append(", ").AppendFullTypeName(item);

        //sb.Append(">(), ").Append(model.UpdateOrder).AppendLine(");");
        sb.AppendLine(">());");
        foreach (var attrType in model.Attributes)
        {
            sb.Append("        GenerationServices.RegisterUpdateMethodAttribute(")
            .Append("typeof(")
            .AppendFullTypeName(attrType)
            .Append("), typeof(")
            .AppendFullTypeName(model.FullName)
            .AppendLine("));");
        }
        if (model.Flagged(UpdateModelFlags.Initable))
        {
            sb.Append("        GenerationServices.RegisterInit<")
            .AppendFullTypeName(model.FullName)
            .AppendLine(">();");
        }
        if (model.Flagged(UpdateModelFlags.Destroyable))
        {
            sb.Append("        GenerationServices.RegisterDestroy<")
            .AppendFullTypeName(model.FullName)
            .AppendLine(">();");
        }

        static (int Start, int Count) ExtractUpdaterName(string interfaceName)
        {
            return (1, interfaceName.Length - "IComponent".Length);
        }
    }

    private static SourceOutput GenerateRegisterGenericType(in ComponentUpdateItemModel model, CancellationToken ct)
    {
        //NOTE:
        //this needs to support older lang versions because unity
        Debug.Assert(model.Flagged(UpdateModelFlags.IsGeneric));

        StringBuilder sb = new();

        string namespaceIndentation;
        string @namespace;
        string @name;
        int sep = model.FullName.LastIndexOf('.');

        if(sep == -1)
        {//global namespace
            @namespace = namespaceIndentation = string.Empty;
            name = model.FullName;
        }
        else
        {
            @namespace = model.FullName.Substring(0, sep);
            @name = model.FullName.Substring(sep + 1);
            namespaceIndentation = "    ";
        }

        sb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using global::Frent.Updating;")
            .AppendLine("using global::Frent.Updating.Runners;")
            .AppendLine();

        if (@namespace != string.Empty)
            sb
                .Append("namespace ").Append(@namespace).AppendLine()
                .AppendLine("{");

        sb
            .AppendLine()
            .Append(namespaceIndentation).Append("partial ").Append(model.Flagged(UpdateModelFlags.IsRecord) ? "record " : string.Empty).Append(model.Flagged(UpdateModelFlags.IsStruct) ? "struct " : "class ").AppendLine(@name)
            .Append(namespaceIndentation).AppendLine("{")
                //TODO: figure out a better way to have user static constructors
                //.AppendLine("    static partial void StaticConstructor();")
                .Append(namespaceIndentation).Append("    static ").Append(model.Type).AppendLine("()")
                .Append(namespaceIndentation).AppendLine("    {");

        AppendInitalizationMethodBody(sb, model);

        sb
            //.AppendLine("        StaticConstructor();")
            .Append(namespaceIndentation).AppendLine("    }")
            .Append(namespaceIndentation).AppendLine("}");

        if (@namespace != string.Empty)
            sb.AppendLine("}");

        string source = sb.ToString();

        sb.Clear().Append(model.Type).Append(".g.cs");

        return new(sb.ToString(), source, model.Diagnostic);
    }

    static bool Launched = false;

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    [DebuggerHidden]
    internal static void LaunchDebugger()
    {
        if (!Debugger.IsAttached && !Launched)
            Debugger.Launch();
        Launched = true;
    }

    private static bool InheritsFromBase(INamedTypeSymbol? typeSymbol, string baseTypeName)
    {
        while (typeSymbol != null)
        {
            if (typeSymbol.ToDisplayString() == baseTypeName)
                return true;
            typeSymbol = typeSymbol.BaseType;
        }
        return false;
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string interfaceName)
    {
        foreach(var i in typeSymbol.AllInterfaces)
        {
            if(i.ToString() == interfaceName)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPartial(INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.DeclaringSyntaxReferences
            .Select(syntaxRef => syntaxRef.GetSyntax() as TypeDeclarationSyntax)
            .Any(syntax => syntax?.Modifiers.Any(SyntaxKind.PartialKeyword) ?? false);
    }

    private static bool InterfaceImplementsIComponent(INamedTypeSymbol namedTypeSymbol) =>
        (namedTypeSymbol.Interfaces.Length == 1 &&
        namedTypeSymbol.Interfaces[0].ConstructedFrom.ToString() == RegistryHelpers.FullyQualifiedTargetInterfaceName) ||
        namedTypeSymbol.Interfaces.Length == 0 &&
        namedTypeSymbol.ConstructedFrom.ToString() == RegistryHelpers.FullyQualifiedTargetInterfaceName;

    internal record struct ComponentUpdateItemModel(UpdateModelFlags Flags, string FullName, string Type, string ImplInterface, string BaseNamespace, string SubNamespace, EquatableArray<string> GenericArguments, EquatableArray<string> Attributes, Diagnostic? Diagnostic)
    {
        public readonly bool Flagged(UpdateModelFlags updateModelFlags) => Flags.HasFlag(updateModelFlags);
    }
    internal record struct SourceOutput(string? Name, string Source, Diagnostic? Diagnostic);
    
    [Flags]
    internal enum UpdateModelFlags
    {
        IsClass = 1 << 0,
        IsStruct = 1 << 1,
        IsGeneric = 1 << 2,
        Initable = 1 << 3,
        Destroyable = 1 << 4,
        IsRecord = 1 << 5,
    }
}