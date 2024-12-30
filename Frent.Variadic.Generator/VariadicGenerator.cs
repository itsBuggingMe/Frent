﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Frent.Variadic.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class VariadicGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(
                ctx => ctx.AddSource("VariadicAttribute.g.cs", Helpers.AttributeString));

            var t = context.SyntaxProvider.ForAttributeWithMetadataName<(ISymbol Symbol, SyntaxNode Node, SemanticModel Model)>(Helpers.AttributeMetadataString,
                [method: DebuggerHidden] (_, _) => true,
                (t, ct) => (t.TargetSymbol, t.TargetNode, t.SemanticModel))
                .Collect()
                .SelectMany(GroupAttributesIntoModels)
                .SelectMany(GenerateCode);

            context.RegisterImplementationSourceOutput(t, (ctx, s) => ctx.AddSource(s.FileName, s.Code));
        }

        [ThreadStatic]
        private static Dictionary<(TypeDeclarationSyntax, ISymbol), (string From, string Pattern)[]> _classTable = new();

        static ImmutableArray<GenerationModel> GroupAttributesIntoModels(ImmutableArray<(ISymbol Symbol, SyntaxNode Node, SemanticModel Model)> variadics, CancellationToken ct)
        {
            _classTable ??= new();
            var table = _classTable;

            foreach (var item in variadics)
            {
                if (item.Node is null)
                    continue;
                var parentType = item.Node?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (parentType is null || item.Symbol is null)
                    continue;

                if (!table.TryGetValue((parentType, item.Symbol), out var stack))
                {
                    table[(parentType, item.Symbol)] = ExtractArguments(item.Symbol);
                }
            }

            var arr = _classTable.Select(kvp =>
            {
                CodeBuilder cb = new CodeBuilder(0);

                cb.AppendLine(Helpers.AutoGenerated)
                    .AppendLine()
                    .Loop(EnumerateUsings(kvp.Key.Item1.SyntaxTree.GetRoot(ct)), (c, s) => c.AppendLine(s.ToString()), ct)
                    .AppendLine()
                    .Append("namespace ").Append(kvp.Key.Item2.ContainingNamespace).Append(';');


                cb.Append(Regex.Replace(kvp.Key.Item1.ToFullString(), @"\[Variadic(?:[^\[\]]|\[[^\[\]]*\]|\([^\(\)]*\))*\]", ""));

                var str = cb.ToString();


                string fileSafeTypeName = kvp.Key.Item2.Name;
                if (fileSafeTypeName.IndexOf('<') is { } t && t != -1)
                    fileSafeTypeName = fileSafeTypeName.Substring(0, t);

                return new GenerationModel()
                {
                    SourceCode = cb.ToString(),
                    FileName = fileSafeTypeName,
                    Attributes = new EquatableArray<(string From, string Pattern)>(kvp.Value)
                };
            }).ToImmutableArray();
            table.Clear();

            return arr;
        }

        static (string, string)[] ExtractArguments(ISymbol symbol)
        {
            var att = symbol.GetAttributes();
            (string, string)[] output = new (string, string)[att.Length];

            for (int i = 0; i < att.Length; i++)
            {
                AttributeData @this = att[i];
                output[i] = ((string)@this.ConstructorArguments[0].Value!, (string)@this.ConstructorArguments[1].Value!);
            }

            return output;
        }

        static ImmutableArray<(string Code, string FileName)> GenerateCode(GenerationModel ctx, CancellationToken ct)//omg cs ref
        {
            CodeBuilder shared = new(-1);
            string code = ctx.SourceCode;

            const int MaxArity = 16;
            var builder = ImmutableArray.CreateBuilder<(string, string)>(MaxArity - 1);
            for (int arity = 2; arity <= MaxArity; arity++)
            {
                string thisRunCode = code;
                foreach (var transform in ctx.Attributes)
                {
                    thisRunCode = Run(thisRunCode, transform.From, transform.Pattern, arity);
                }

                builder.Add((thisRunCode, $"{ctx.FileName}.{arity}.cs"));
            }

            return builder.ToImmutableArray();
        }

        [ThreadStatic]
        static StringBuilder replace = new StringBuilder();

        internal static string Run(string cleanSource, string from, string pattern, int arity)
        {
            replace ??= new();
            replace.Clear();

            ExtractPattern(pattern.AsSpan(), out var prologue, out var epilogue);
            replace.Append(prologue);
            pattern = pattern.Substring(prologue.Length + 1, pattern.Length - epilogue.Length - prologue.Length - 2);

            string segment = string.Empty;
            for (int i = 1; i <= arity; i++)
                replace.Append(segment = pattern.Replace("$", i.ToString()));
            int index;
            if ((index = segment.IndexOf(',')) != -1 && segment.Length - index < 3)
            {
                replace.Remove(replace.Length - (segment.Length - index), segment.Length - index);
            }

            replace.Append(epilogue);

            return cleanSource.Replace(from, replace.ToString());
        }

        private static void ExtractPattern(ReadOnlySpan<char> pattern, out ReadOnlySpan<char> prologue, out ReadOnlySpan<char> epilogue)
        {
            prologue = pattern.Slice(0, pattern.IndexOf('|'));
            epilogue = pattern.Slice(pattern.LastIndexOf('|') + 1);
        }

        static IEnumerable<UsingDirectiveSyntax> EnumerateUsings(SyntaxNode root)
        {
            foreach (var item in root.ChildNodes())
            {
                if (item is UsingDirectiveSyntax usingDirectiveSyntax)
                {
                    yield return usingDirectiveSyntax;
                }
            }
        }

        internal struct GenerationModel
        {
            public string SourceCode;
            public string FileName;
            public EquatableArray<(string From, string Pattern)> Attributes;
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
    }
}