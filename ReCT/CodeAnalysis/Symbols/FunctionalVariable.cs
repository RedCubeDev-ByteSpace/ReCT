namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class FunctionalVariableSymbol : VariableSymbol
    {
        internal FunctionalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, bool isVirtual)
            : base(name, isReadOnly, true, true, type)
        {
            IsVirtual = isVirtual;
        }

        public override SymbolKind Kind => SymbolKind.FunctionalVariable;

        public bool IsVirtual { get; }
    }
}