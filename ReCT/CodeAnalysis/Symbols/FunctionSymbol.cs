using System.Collections.Immutable;
using ReCT.CodeAnalysis.Syntax;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FunctionDeclarationSyntax declaration = null, bool isPublic = false, string package = "", bool isVirtual = false, bool isOverride = false, Text.TextLocation location = new Text.TextLocation())
            : base(name, location)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
            IsPublic = isPublic;
            Package = package;
            IsVirtual = isVirtual;
            IsOverride = isOverride;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public FunctionDeclarationSyntax Declaration { get; }
        public bool IsPublic { get; }
        public string Package { get; }
        public bool IsVirtual { get; }
        public bool IsOverride { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
		public ReCT.CodeAnalysis.Binding.BoundScope scope;
		public ReCT.CodeAnalysis.Binding.BoundBlockStatement block;
    }
}