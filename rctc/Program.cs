using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReCT.CodeAnalysis;
using ReCT.CodeAnalysis.Syntax;
using ReCT.IO;
using Mono.Options;
using System.Collections.Immutable;

namespace ReCT
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-------------------------------");
            Console.WriteLine("ReCT Standalone Compiler " + info.Version);
            Console.WriteLine("-------------------------------");
            Console.ForegroundColor = ConsoleColor.White;

            string outputPath = default;
            string moduleName = default;

            bool helpRequested = false;

            List<string> referencePaths = new List<string>();
            List<string> sourcePaths = new List<string>();

            OptionSet options = new OptionSet
            {
                "usage: msc <source-paths> [options]",
                { "r=", "The {path} of an assembly to reference", v => referencePaths.Add(v) },
                { "o=", "The output {path} of the assembly to create", v => outputPath = v },
                { "m=", "The {name} of the module", v => moduleName = v },
                { "?|h|help", "Prints help", v => helpRequested = true },
                { "<>", v => sourcePaths.Add(v) }
            };

            options.Parse(args);

            if (helpRequested)
                options.WriteOptionDescriptions(Console.Out);

            if (sourcePaths.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: need at least one source file");
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (outputPath == null)
                outputPath = Path.ChangeExtension(sourcePaths[0], ".exe");

            if (moduleName == null)
                moduleName = Path.GetFileNameWithoutExtension(outputPath);

            SyntaxTree[] syntaxTrees = new SyntaxTree[sourcePaths.Count];

            for (int pathIndex = 0; pathIndex < sourcePaths.Count; pathIndex++)
            {
                if (!File.Exists(sourcePaths[pathIndex]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: file '{sourcePaths[pathIndex]}' doesn't exist");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                SyntaxTree syntaxTree = SyntaxTree.Load(sourcePaths[pathIndex]);
                syntaxTrees[pathIndex] = syntaxTree;
            }

            for (int pathIndex = 0; pathIndex < referencePaths.Count; pathIndex++)
                if (!File.Exists(referencePaths[pathIndex]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: file '{referencePaths[pathIndex]}' doesn't exist");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

            Compilation compilation = Compilation.Create(syntaxTrees.ToArray());
            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(moduleName, referencePaths.ToArray(), outputPath);

            if (diagnostics.Length != 0)
                Console.Error.WriteDiagnostics(diagnostics);
        }
    }
}
