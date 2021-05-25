using System.Collections.Generic;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class EnumSymbol : Symbol
    {
        public EnumSymbol(string name, Dictionary<string, int> values)
            : base(name)
        {
            Values = values;
        }   

        public Dictionary<string, int> Values { get; }
        public override SymbolKind Kind => SymbolKind.Enum;
    }
}