using System;

namespace ReCT.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, bool isGlobal, TypeSymbol type)
            : base(name)
        {
            IsReadOnly = isReadOnly;
            IsGlobal = isGlobal;
            Type = type;
        }

        public bool IsReadOnly { get; }
        public bool IsGlobal { get; }
        public TypeSymbol Type { get; }
    }
}