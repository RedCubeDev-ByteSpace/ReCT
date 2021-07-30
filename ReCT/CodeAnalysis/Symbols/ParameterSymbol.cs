namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public ParameterSymbol(string name, TypeSymbol type, int ordinal, Text.TextLocation location = new Text.TextLocation())
            : base(name, isReadOnly: true, type, location)
        {
            Ordinal = ordinal;
        }

        public override SymbolKind Kind => SymbolKind.Parameter;
        public int Ordinal { get; }
    }
}