﻿using Frent.Variadic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Frent.Generator;

[Generator(LanguageNames.CSharp)]
public class ComponentUpdateTypeRegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(static (n, _) => n is TypeDeclarationSyntax typeDec && typeDec.BaseList is not null,
            static (gsc, ct) =>
            {

                INamedTypeSymbol? symbol = gsc.SemanticModel.GetDeclaredSymbol(gsc.Node, ct) as INamedTypeSymbol;

                if (symbol is not null)
                {
                    foreach (var @interface in symbol.AllInterfaces)
                    {
                        if (InterfaceImplementsIComponent(@interface))
                        {
                            string @namespace = symbol.ContainingNamespace.ToString();
                            if (@namespace == "<global namespace>")
                                @namespace = string.Empty;
                            int index = @namespace.IndexOf('.');
                            var genericArgs = @interface.TypeArguments.Length == 0 ? [] : new string[@interface.TypeArguments.Length];

                            for (int i = 0; i < @interface.TypeArguments.Length; i++)
                                genericArgs[i] = @interface.TypeArguments[i].ToString();

                            //stack allocate 6 slots
                            var stackAttributes = new StackStack<string>([null!, null!, null!, null!, null!, null!]);
                            bool initable = ImplementsInterface(symbol, RegistryHelpers.FullyQualifiedInitableInterfaceName);
                            bool destroyable = ImplementsInterface(symbol, RegistryHelpers.FullyQualifiedDestroyableInterfaceName);
                            
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
                                initable,
                                destroyable,
                                symbol.ToString(),
                                symbol.Name,
                                @interface.Name,
                                index == -1 ? @namespace : @namespace.Substring(0, index),
                                index == -1 ? string.Empty : @namespace.Substring(index + 1),
                                new EquatableArray<string>(genericArgs),
                                new EquatableArray<string>(stackAttributes.ToArray()));
                        }
                    }
                }

                return new ComponentUpdateItemModel(false, false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new([]), new([]));
            });

        IncrementalValuesProvider<ComponentUpdateItemModel> types = models
            .Where(m => m.Type.Length != 0);

#if UNITY
        //netstandard generation
        IncrementalValueProvider<(string? Name, string Source)> monolith = types
            .Collect()
            .Select(GenerateMonolithicRegistrationFile);

        context.RegisterImplementationSourceOutput(monolith, (ctx, s) =>
        {
            if (s.Name is not null)
                ctx.AddSource(s.Name, s.Source);
        });
#else
        //normal generation
        IncrementalValuesProvider<(string? Name, string Source)> files = types
            .Select(GenerateModuleInitalizerFiles);

        context.RegisterImplementationSourceOutput(files, (ctx, s) =>
        {
            if (s.Name is not null)
                ctx.AddSource(s.Name, s.Source);
        });
#endif
    }

    private static (string? Name, string Source) GenerateMonolithicRegistrationFile(ImmutableArray<ComponentUpdateItemModel> models, CancellationToken ct)
    {
        StringBuilder sb = new StringBuilder();


        sb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using Frent.Updating;")
            .AppendLine("using System.Runtime.CompilerServices;")
            .AppendLine();

        sb
            .AppendLine()
            .AppendLine("public static class ComponentRegistry")
            .AppendLine("{")
                .AppendLine("    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
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

        return ("ComponentRegistry.g.cs", source);
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

    private static (string? Name, string Source) GenerateModuleInitalizerFiles(ComponentUpdateItemModel model, CancellationToken ct)
    {
        StringBuilder sb = new StringBuilder();

        sb
            .AppendLine("// <auto-generated />")
            .AppendLine("// This file was auto generated using Frent's source generator")
            .AppendLine("using Frent.Updating;")
            .AppendLine("using System.Runtime.CompilerServices;")
            .AppendLine();

        if (model.BaseNamespace != string.Empty)
            sb
                .Append("namespace ").Append(model.BaseNamespace).Append(';').AppendLine();

        sb
            .AppendLine()
            .Append("internal static partial class ").Append(model.Type).AppendLine("ComponentUpdateInitalizer_")
            .AppendLine("{")
                .AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]")
                .AppendLine("    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]")
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

        return (name, source);
    }

    private static void AppendInitalizationMethodBody(StringBuilder sb, in ComponentUpdateItemModel model)
    {
        var span = ExtractUpdaterName(model.ImplInterface);
        
        sb
            .Append("        GenerationServices.RegisterType(typeof(")
            .AppendNamespace(model.SubNamespace).Append(model.Type).Append("), new Frent.Updating.Runners.");

        (model.ImplInterface == RegistryHelpers.TargetInterfaceName ? sb.Append("None") : sb.Append(model.ImplInterface, span.Start, span.Count))
            .Append("UpdateRunnerFactory")
            .Append('<')
            .AppendNamespace(model.SubNamespace)
            .Append(model.Type);

        foreach (var item in model.GenericArguments)
            sb.Append(", ").Append(item);

        //sb.Append(">(), ").Append(model.UpdateOrder).AppendLine(");");
        sb.AppendLine(">());");
        foreach (var attrType in model.Attributes)
        {
            sb.Append("        GenerationServices.RegisterUpdateMethodAttribute(")
            .Append("typeof(")
            .Append(attrType)
            .Append("), typeof(")
            .AppendNamespace(model.SubNamespace).Append(model.Type)
            .AppendLine("));");
        }
        if (model.Initable)
        {
            sb.Append("        GenerationServices.RegisterInit<")
            .AppendNamespace(model.SubNamespace)
            .Append(model.Type)
            .AppendLine(">();");
        }
        if (model.Destroyable)
        {
            sb.Append("        GenerationServices.RegisterDestroy<")
            .AppendNamespace(model.SubNamespace)
            .Append(model.Type)
            .AppendLine(">();");
        }

        static (int Start, int Count) ExtractUpdaterName(string interfaceName)
        {
            return (1, interfaceName.Length - "IComponent".Length);
        }
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

    private static bool InterfaceImplementsIComponent(INamedTypeSymbol namedTypeSymbol) =>
        (namedTypeSymbol.Interfaces.Length == 1 &&
        namedTypeSymbol.Interfaces[0].ConstructedFrom.ToString() == RegistryHelpers.FullyQualifiedTargetInterfaceName) ||
        namedTypeSymbol.Interfaces.Length == 0 &&
        namedTypeSymbol.ConstructedFrom.ToString() == RegistryHelpers.FullyQualifiedTargetInterfaceName;

    internal record struct ComponentUpdateItemModel(bool Initable, bool Destroyable, string FullName, string Type, string ImplInterface, string BaseNamespace, string SubNamespace, EquatableArray<string> GenericArguments, EquatableArray<string> Attributes);
}