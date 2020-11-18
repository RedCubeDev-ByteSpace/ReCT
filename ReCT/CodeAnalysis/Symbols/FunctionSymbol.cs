using System.Collections.Immutable;
using ReCT.CodeAnalysis.Syntax;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FunctionDeclarationSyntax declaration = null, bool isPublic = false, string package = "")
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
            IsPublic = isPublic;
            Package = package;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public FunctionDeclarationSyntax Declaration { get; }
        public bool IsPublic { get; }
        public string Package { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
    }
}