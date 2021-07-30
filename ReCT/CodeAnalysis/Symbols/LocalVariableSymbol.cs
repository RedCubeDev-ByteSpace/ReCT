namespace ReCT.CodeAnalysis.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, Text.TextLocation location = new Text.TextLocation())
            : base(name, isReadOnly, false, false, type, location)
        {
        }

        public override SymbolKind Kind => SymbolKind.LocalVariable;
    }

}