﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using HedgeModManager.Misc;
using HedgeModManager.Properties;
using Markdig.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace HedgeModManager
{
    public class CodeProvider
    {
        public static string CodesTextPath => Path.Combine(App.ModsDbPath, "Codes.hmm");
        public static string ExtraCodesTextPath => Path.Combine(App.ModsDbPath, "ExtraCodes.hmm");

        public static string CompiledCodesPath => Path.Combine(App.ModsDbPath, "Codes.dll");

        public static MethodDeclarationSyntax LoaderExecutableMethod =
            SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), "IsLoaderExecutable")
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));

        public static UsingDirectiveSyntax[] PredefinedUsingDirectives =
        {
            SyntaxFactory.UsingDirective(
                SyntaxFactory.Token(SyntaxKind.UsingKeyword),
                SyntaxFactory.Token(SyntaxKind.None), null,
                SyntaxFactory.ParseName("System"), SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory.UsingDirective(
                SyntaxFactory.Token(SyntaxKind.UsingKeyword),
                SyntaxFactory.Token(SyntaxKind.None), null,
                SyntaxFactory.ParseName("HMMCodes"), SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory.UsingDirective(
                SyntaxFactory.Token(SyntaxKind.UsingKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword), null,
                SyntaxFactory.ParseName("HMMCodes.MemoryService"), SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        };

        public static SyntaxTree[] PredefinedClasses =
        {
            CSharpSyntaxTree.ParseText(Resources.MemoryService),
            CSharpSyntaxTree.ParseText(Resources.Keys)
        };

        public static void CompileCodes(IEnumerable<Code> sources, string assemblyPath, params string[] loadPaths)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            var trees = from source in sources select source.CreateSyntaxTree();

            var loads = GetLoadAssemblies(sources, loadPaths);

            loads.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            loads.Add(MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location));
            loads.Add(MetadataReference.CreateFromFile(typeof(Component).Assembly.Location));
            loads.Add(MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location));

            var compiler = CSharpCompilation.Create("HMMCodes", trees, loads, options).AddSyntaxTrees(PredefinedClasses);

            using (var stream = File.Create(assemblyPath))
            {
                var result = compiler.Emit(stream);
                if (!result.Success)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Error compiling codes");
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        if(diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            builder.AppendLine(diagnostic.ToString());
                        }
                    }
                    throw new Exception(builder.ToString());
                }
            }
        }

        public static List<MetadataReference> GetLoadAssemblies(IEnumerable<Code> sources, params string[] lookupPaths)
        {
            var meta = new List<MetadataReference>();

            var trees = from source in sources select source.ParseSyntaxTree();
            var basePath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var wpfPath = Path.Combine(basePath, "WPF");

            foreach (var tree in trees)
            {
                var unit = tree.GetCompilationUnitRoot();
                var loads = unit.GetLoadDirectives();
                foreach (var load in loads)
                {
                    var value = load.File.ValueText;
                    var path = Path.Combine(basePath, value);
                    if (File.Exists(path))
                    {
                        meta.Add(MetadataReference.CreateFromFile(path));
                        continue;
                    }

                    path = Path.Combine(wpfPath, value);
                    if (File.Exists(path))
                    {
                        meta.Add(MetadataReference.CreateFromFile(path));
                        continue;
                    }

                    foreach (var lookupPath in lookupPaths)
                    {
                        path = Path.Combine(lookupPath, value);
                        if (File.Exists(path))
                        {
                            meta.Add(MetadataReference.CreateFromFile(path));
                            break;
                        }
                    }
                }
            }
            
            return meta;
        }
    }

    public class OptionalColonRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.SemicolonToken) && token.IsMissing)
            {
                return SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            }
            return base.VisitToken(token);
        }
    }
}
