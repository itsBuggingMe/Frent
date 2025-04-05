using Frent.Generator.Model;
using Frent.Generator.Models;
using Frent.Generator.Structures;
using Frent.Variadic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Frent.Generator;

[Generator(LanguageNames.CSharp)]
public class ComponentUpdateTypeRegistryGenerator : IIncrementalGenerator
{
    public const string Version = "0.5.4.3";
    private const string GlobalNamespace = "<global namespace>";

    private static SymbolDisplayFormat? _symbolDisplayFormat;
    private static SymbolDisplayFormat FullyQualifiedTypeNameFormat => _symbolDisplayFormat ??= new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is TypeDeclarationSyntax typeDec && typeDec.BaseList is not null,
            GenerateComponentUpdateModel);

        IncrementalValueProvider<ImmutableArray<ComponentUpdateItemModel>> allModels = models.Where(m => !m.IsDefault).Collect();

        var mainRegistrationFile = allModels.Select(
            (im, ct) => 
                GenerateMonolithicRegistrationFile(im.Where(c => !c.HasFlag(UpdateModelFlags.IsSelfInit)).ToImmutableArray(), ct)
            );

        context.RegisterImplementationSourceOutput(mainRegistrationFile, RegisterSource);

        var genericComponentFiles = models
            .Where(c => c.HasFlag(UpdateModelFlags.IsSelfInit))
            .Select(GenerateRegisterGenericType);
        
        context.RegisterImplementationSourceOutput(genericComponentFiles, RegisterSource);

        static void RegisterSource(SourceProductionContext context, SourceOutput output)
        {
            if (output.Name is not null)
                context.AddSource(output.Name, output.Source);
            if(output.Diagnostics is { } diags)
                foreach(var e in diags)
                    context.ReportDiagnostic(e);
        }
    }
    
    private static ComponentUpdateItemModel GenerateComponentUpdateModel(GeneratorSyntaxContext gsc, CancellationToken ct)
    {
        if (gsc.SemanticModel.GetDeclaredSymbol(gsc.Node, ct) is not INamedTypeSymbol componentTypeSymbol)
            return ComponentUpdateItemModel.Default;
        if (componentTypeSymbol.TypeKind != TypeKind.Class && componentTypeSymbol.TypeKind != TypeKind.Struct)
            return ComponentUpdateItemModel.Default;

        UpdateModelFlags flags = UpdateModelFlags.None;
        Stack<Diagnostic> diagnostics = new Stack<Diagnostic>(1);
        INamedTypeSymbol? @interface = null;

        string[] genericArguments = [];
        bool needsRegistering = false;

        foreach (var potentialInterface in componentTypeSymbol.AllInterfaces)
        {
            ct.ThrowIfCancellationRequested();

            if (!ImplementsOrIsInterface(potentialInterface, RegistryHelpers.FullyQualifiedTargetInterfaceName))
                continue;
            //potentialInterface is some kind of IComponentBase

            string name = potentialInterface.ToString();

            needsRegistering = true;

            if (IsSpecialInterface(name))
            {
                if(name != RegistryHelpers.FullyQualifiedTargetInterfaceName)
                {
                    flags |= name switch
                    {
                        RegistryHelpers.FullyQualifiedInitableInterfaceName => UpdateModelFlags.Initable,
                        RegistryHelpers.FullyQualifiedDestroyableInterfaceName => UpdateModelFlags.Destroyable,
                        _ => UpdateModelFlags.None,
                    };
                }
                else
                {
                    @interface ??= potentialInterface;
                }
            }
            else if(IsFrentComponentInterface(name))
            {
                if(@interface is not null && !IsSpecialInterface(@interface.ToString()))
                {
                    diagnostics.Push(CreateDiagnostic(componentTypeSymbol, 3, "Multiple Component Interface Implementations", "Components should only implement one update component interface.", DiagnosticSeverity.Warning));
                }

                @interface = potentialInterface;

                if(@interface.TypeArguments.Length != 0)
                {
                    genericArguments = new string[@interface.TypeArguments.Length];

                    for (int i = 0; i < @interface.TypeArguments.Length; i++)
                    {
                        ITypeSymbol namedTypeSymbol = @interface.TypeArguments[i];
                        genericArguments[i] = namedTypeSymbol.ToDisplayString(FullyQualifiedTypeNameFormat);
                    }
                }
            }
        }

        //this path is still hot!
        if (!needsRegistering || @interface is null)
            return ComponentUpdateItemModel.Default;

        //only components here

        //since inline array doesn't exist, [null!, ...] allocates -_-
        Stack<string> attributes = new Stack<string>(1);
        PushUpdateTypeAttributes(ref attributes, gsc.Node, gsc.SemanticModel);

        AddMiscFlags();

        Debug.Assert(genericArguments is not null);

        string? @namespace = componentTypeSymbol.ContainingNamespace?.ToString();

        if (@namespace == GlobalNamespace)
            @namespace = null;

        var nestTypes = GetContainingTypes(ref diagnostics, out bool isAcc);

        if ((nestTypes.Length != 0 && !isAcc) || flags.HasFlag(UpdateModelFlags.IsGeneric))
            flags |= UpdateModelFlags.IsSelfInit;

        //self init component must be partial
        if (flags.HasFlag(UpdateModelFlags.IsSelfInit) && !IsPartial(componentTypeSymbol))
        {
            if(flags.HasFlag(UpdateModelFlags.IsGeneric))
            {
                diagnostics.Push(CreateDiagnostic(componentTypeSymbol, 0, "Non-partial Generic Component Type", $"Generic Component '{componentTypeSymbol.Name}' must be marked as partial."));
            }
            else
            {
                diagnostics.Push(CreateDiagnostic(componentTypeSymbol, 2, "Non-partial Nested Inaccessible Component Type", $"Inaccessible Nested Component Type '{componentTypeSymbol.Name}' must be marked as partial."));
            }
        }

        return new ComponentUpdateItemModel(

            Flags: flags,
            FullName: componentTypeSymbol.ToString(),
            Namespace: @namespace,
            ImplInterface:  @interface.Name,
            HintName: componentTypeSymbol.Name,
            MinimallyQualifiedName: componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),

            NestedTypes: new EquatableArray<TypeDeclarationModel>(nestTypes),
            GenericArguments: new EquatableArray<string>(genericArguments!),
            Attributes: new EquatableArray<string>(attributes.ToArray()),

            Diagnostics: new EquatableArray<Diagnostic>(diagnostics.ToArray())
            );

        void AddMiscFlags()
        {
            if (componentTypeSymbol.IsGenericType)
                flags |= UpdateModelFlags.IsGeneric;

            if (componentTypeSymbol.TypeKind == TypeKind.Class)
                flags |= UpdateModelFlags.IsClass;
            else if (componentTypeSymbol.TypeKind == TypeKind.Struct)
                flags |= UpdateModelFlags.IsStruct;

            if (componentTypeSymbol.IsRecord)
                flags |= UpdateModelFlags.IsRecord;
        }

        TypeDeclarationModel[] GetContainingTypes(ref Stack<Diagnostic> diags, out bool componentTypeIsAcsessableFromModule)
        {
            componentTypeIsAcsessableFromModule =
                componentTypeSymbol.DeclaredAccessibility == Accessibility.Public ||
                componentTypeSymbol.DeclaredAccessibility == Accessibility.Internal;

            int nestedTypeCount = 0;
            INamedTypeSymbol current = componentTypeSymbol;
            while (current.ContainingType is not null)
            {
                current = current.ContainingType;

                if (!componentTypeIsAcsessableFromModule && !IsPartial(current))
                    diags.Push(CreateDiagnostic(componentTypeSymbol, 1, "Non-partial Outer Inaccessible Type", $"Outer type of inaccessible nested component type '{current.Name}' must be marked as partial."));

                nestedTypeCount++;
            }
            TypeDeclarationModel[] nestedTypeSymbols = new TypeDeclarationModel[nestedTypeCount];
            current = componentTypeSymbol;
            int index = 0;
            while (current.ContainingType is not null)
            {
                current = current.ContainingType;    
                nestedTypeSymbols[index++] = new TypeDeclarationModel(
                    current.IsRecord,
                    current.TypeKind,
                    current.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                    );
            }
            nestedTypeSymbols.AsSpan().Reverse();
            return nestedTypeSymbols;
        }
    }

    private static void PushUpdateTypeAttributes(ref Stack<string> attributes,  SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var item in ((TypeDeclarationSyntax)node).Members)
        {
            if (item is MethodDeclarationSyntax method && method.AttributeLists.Count != 0 && method.Identifier.ToString() == RegistryHelpers.UpdateMethodName)
            {
                foreach (var attrList in method.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        if (semanticModel.GetSymbolInfo(attr).Symbol is IMethodSymbol attrCtor)
                        {
                            if (InheritsFromBase(attrCtor.ContainingType, RegistryHelpers.UpdateTypeAttributeName))
                            {
                                attributes.Push(attrCtor.ContainingType.ToString());
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
    }

    private static Diagnostic CreateNeedPartialDiag(INamedTypeSymbol componentTypeSymbol)
    {
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id: "FR0000",
                title: "Non-partial Generic Component Type",
                messageFormat: "Generic Component '{0}' must be marked as partial.",
                category: "Source Generation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true
                ),
            componentTypeSymbol.Locations.First(),
            componentTypeSymbol.Name);
    }

    private static Diagnostic CreateDiagnostic(ISymbol symbol, int id, string title, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id: $"FR{id:D4}",
                title: title,
                messageFormat: message,
                category: "Source Generation",
                severity,
                isEnabledByDefault: true
                ),
            symbol.Locations.First());
    }

    private static SourceOutput GenerateMonolithicRegistrationFile(ImmutableArray<ComponentUpdateItemModel> models, CancellationToken ct)
    {
        if (models.Length == 0)
            return new(default, string.Empty, default);

        CodeBuilder cb = CodeBuilder.ThreadShared;

        cb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using global::Frent.Updating;")
            .AppendLine("using global::Frent.Updating.Runners;")
            .AppendLine("using global::System.Runtime.CompilerServices;")
            .AppendLine()
            .AppendLine("namespace Frent.Generator")
            .Scope()
                .AppendLine()
                .Append("[global::System.CodeDom.Compiler.GeneratedCode(\"Frent.Generator\", \"").Append(Version).AppendLine("\")]")
                .AppendLine("[global::System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
                .AppendLine("internal static class FrentComponentRegistry")
                .Scope()
#if UNITY
                    .AppendLine("[global::UnityEngine.RuntimeInitializeOnLoadMethod]")
#else
                    .AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]")
#endif
                    .AppendLine("[global::System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
                    .AppendLine("internal static void RegisterAll()")
                    .Scope()
                        .Foreach(models.AsSpan(), ct, static (in ComponentUpdateItemModel model, CodeBuilder builder, CancellationToken ct) =>
                        {
                            AppendInitalizationMethodBody(builder, in model);
                            ct.ThrowIfCancellationRequested();
                        })
                    .Unscope()
                .Unscope()
            .Unscope();

        string source = cb.ToString();
        cb.Clear();

        return new("FrentComponentRegistry.g.cs", source, null);
    }
    
    private static void AppendInitalizationMethodBody(CodeBuilder cb, in ComponentUpdateItemModel model)
    {
        var span = ExtractUpdaterName(model.ImplInterface);
        
        cb
            .Append("GenerationServices.RegisterType(typeof(")
            .Append("global::").Append(model.FullName)
            .Append("), new ");

        (model.ImplInterface == RegistryHelpers.TargetInterfaceName ? cb.Append("None") : cb.Append(model.ImplInterface, span.Start, span.Count))
            .Append("UpdateRunnerFactory")
            .Append('<')
            .Append("global::").Append(model.FullName);

        foreach (var item in model.GenericArguments)
            cb.Append(", ").Append(item);

        //sb.Append(">(), ").Append(model.UpdateOrder).AppendLine(");");
        cb.AppendLine(">());");
        foreach (var attrType in model.Attributes)
        {
            cb.Append("GenerationServices.RegisterUpdateMethodAttribute(")
            .Append("typeof(")
            .Append("global::").Append(attrType)
            .Append("), typeof(")
            .Append("global::").Append(model.FullName)
            .AppendLine("));");
        }
        if (model.HasFlag(UpdateModelFlags.Initable))
        {
            cb.Append("GenerationServices.RegisterInit<")
            .Append("global::").Append(model.FullName)
            .AppendLine(">();");
        }
        if (model.HasFlag(UpdateModelFlags.Destroyable))
        {
            cb.Append("GenerationServices.RegisterDestroy<")
            .Append("global::").Append(model.FullName)
            .AppendLine(">();");
        }

        cb.AppendLine();

        static (int Start, int Count) ExtractUpdaterName(string interfaceName)
        {
            return (1, interfaceName.Length - "IComponent".Length);
        }
    }

    static bool IsSpecialInterface(string @fullyQualifiedName)
    {
        bool isSpecial =
            @fullyQualifiedName == RegistryHelpers.FullyQualifiedTargetInterfaceName ||
            RegistryHelpers.FullyQualifiedInitableInterfaceName == @fullyQualifiedName ||
            RegistryHelpers.FullyQualifiedDestroyableInterfaceName == @fullyQualifiedName
            ;
        return isSpecial;
    }

    static bool IsFrentComponentInterface(string @fullyQualifiedName)
    {
        return fullyQualifiedName.StartsWith(RegistryHelpers.FrentComponentNamespace);
    }

    private static SourceOutput GenerateRegisterGenericType(ComponentUpdateItemModel model, CancellationToken ct)
    {
        //NOTE:
        //this needs to support older lang versions because unity

        CodeBuilder cb = CodeBuilder.ThreadShared;

        string? @namespace = model.Namespace;

        cb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using global::Frent.Updating;")
            .AppendLine("using global::Frent.Updating.Runners;")
            .AppendLine("using global::System.Runtime.CompilerServices;")
            .AppendLine()
            .If(@namespace is not null, @namespace, (ns, c) => c.Append("namespace ").AppendLine(ns).Scope())

                .Foreach((ReadOnlySpan<TypeDeclarationModel>)model.NestedTypes, ct, 
                (in TypeDeclarationModel typeInfo, CodeBuilder cb, CancellationToken _) => 
                    cb.Append("partial ").If(typeInfo.IsRecord, c => c.Append("record")).Append(typeInfo.TypeKind switch
                    {
                        TypeKind.Struct => "struct ",
                        TypeKind.Class => "class ",
                        TypeKind.Interface => "interface ",
                        _ => throw new NotImplementedException()
                    }).AppendLine(typeInfo.Name).Scope())

                    .Append("partial ").If(model.IsRecord, c => c.Append("record ")).Append(model.IsStruct ? "struct " : "class ").Append(model.MinimallyQualifiedName).AppendLine()
                    .Scope()
                        .Append("static ").Append(model.HintName).AppendLine("()")
                        .Scope()
                            .Execute(in model, ct, (in ComponentUpdateItemModel model, CodeBuilder builder, CancellationToken ct) => AppendInitalizationMethodBody(cb, in model))
                        .Unscope()
                    .Unscope()

                .Foreach((ReadOnlySpan<TypeDeclarationModel>)model.NestedTypes, ct, (in TypeDeclarationModel s, CodeBuilder cb, CancellationToken _) => cb.Unscope())

            .If(@namespace is not null, c => c.Unscope());

        return new(SanitizeNameForFile(model.FullName), cb.ToString(), model.Diagnostics);

        static string SanitizeNameForFile(string name)
        {
            const string FileEnd = ".g.cs";
            Span<char> newName = stackalloc char[name.Length + FileEnd.Length];
            for(int i = 0; i < name.Length; i++)
            {
                newName[i] = name[i] switch
                {
                    '<' or '>' => '_',
                    _ => name[i],
                };
            }
            FileEnd.AsSpan().CopyTo(newName.Slice(name.Length));
            string res = newName.ToString();
            return res;
        }
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

    private static bool ImplementsOrIsInterface(INamedTypeSymbol typeSymbol, string fullyQualifiedInterfaceName)
    {
        if(typeSymbol.ToString() == fullyQualifiedInterfaceName)
        {
            return true;
        }

        foreach(var i in typeSymbol.AllInterfaces)
        {
            if(i.ToString() == fullyQualifiedInterfaceName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.DeclaringSyntaxReferences
            .Select(syntaxRef => syntaxRef.GetSyntax() as TypeDeclarationSyntax)
            .Any(syntax => syntax?.Modifiers.Any(SyntaxKind.PartialKeyword) ?? false);
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    [DebuggerHidden]
    internal static void LaunchDebugger()
    {
        if (!Debugger.IsAttached && !Launched)
            Debugger.Launch();
        Launched = true;
    }
    static bool Launched = false;
}