namespace ReCT.CodeAnalysis.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, bool isReadOnly, TypeSymbol type)
            : base(name, isReadOnly, false, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.LocalVariable;
    }

}