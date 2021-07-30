using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class TypeFunctionSymbol : Symbol
    {
        public TypeFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol childtype, TypeSymbol type)
            : base(name, new Text.TextLocation())
        {
            Parameters = parameters;
            Childtype = childtype;
            Type = type;
        }

        public override SymbolKind Kind => SymbolKind.TypeFunction;

        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Childtype { get; }
        public TypeSymbol Type { get; }
    }
}