using Frent.Variadic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;
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
                            int order = 0;

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
                                order,
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

                return new ComponentUpdateItemModel(-1, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new([]), new([]));
            });

        IncrementalValuesProvider<(string Name, string Source)> file = models
            .Where(m => m.Type.Length != 0)
            .Select(GenerateFiles);

        context.RegisterImplementationSourceOutput(file, (ctx, s) => ctx.AddSource(s.Name, s.Source));
    }

    private static (string Name, string Source) GenerateFiles(ComponentUpdateItemModel model, CancellationToken ct)
    {
        StringBuilder sb = new StringBuilder();

        var span = ExtractUpdaterName(model.ImplInterface);

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
                .AppendLine("    {")
                .Append("        GenerationServices.RegisterType(typeof(")
                .AppendNamespace(model.SubNamespace).Append(model.Type).Append("), new Frent.Updating.Runners.");
        (model.ImplInterface == RegistryHelpers.TargetInterfaceName ? sb.Append("None") : sb.Append(model.ImplInterface, span.Start, span.Count)).Append("UpdateRunnerFactory").Append('<').AppendNamespace(model.SubNamespace).Append(model.Type);

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

        //end method ^& class
        sb.AppendLine("    }");
        sb.AppendLine("}");

        string source = sb.ToString();
        sb.Clear();

        sb.AppendNamespace(model.SubNamespace).Append(model.Type).Append("ComponentUpdateInitalizer").Append(".g.cs");

        string name = sb.ToString();
        sb.Clear();

        return (name, source);

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

    internal record struct ComponentUpdateItemModel(int UpdateOrder, string FullName, string Type, string ImplInterface, string BaseNamespace, string SubNamespace, EquatableArray<string> GenericArguments, EquatableArray<string> Attributes);
}