namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class FunctionalVariableSymbol : VariableSymbol
    {
        internal FunctionalVariableSymbol(string name, bool isReadOnly, TypeSymbol type)
            : base(name, isReadOnly, true, true, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.FunctionalVariable;
    }
}