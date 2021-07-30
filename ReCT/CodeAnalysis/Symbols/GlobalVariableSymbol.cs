namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, Text.TextLocation location = new Text.TextLocation())
            : base(name, isReadOnly, true, false, type, location)
        {
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}