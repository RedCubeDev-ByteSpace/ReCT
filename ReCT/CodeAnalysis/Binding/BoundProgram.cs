using System.Collections.Immutable;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram(BoundProgram previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol mainFunction,
                            FunctionSymbol scriptFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
                            ImmutableDictionary<ClassSymbol, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>> classes,
                            ImmutableArray<Package.Package> packages,
                            ImmutableArray<EnumSymbol> enums,
                            string _namespace, string _type)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
            Classes = classes;
            Packages = packages;
            Namespace = _namespace;
            Type = _type;
            Enums = enums;
        }

        public BoundProgram Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol MainFunction { get; }
        public FunctionSymbol ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public ImmutableDictionary<ClassSymbol, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>> Classes { get; }
        public ImmutableArray<Package.Package> Packages { get; }
        public ImmutableArray<EnumSymbol> Enums { get; }
        public string Namespace { get; }
        public string Type { get; }
    }
}
