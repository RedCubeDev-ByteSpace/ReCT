using System.Collections.Generic;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class EnumSymbol : Symbol
    {
        public EnumSymbol(string name, Dictionary<string, int> values, TypeSymbol type)
            : base(name)
        {
            Values = values;
            Type = type;
        }   

        public Dictionary<string, int> Values { get; }
        public TypeSymbol Type { get; }
        public override SymbolKind Kind => SymbolKind.Enum;
    }
}