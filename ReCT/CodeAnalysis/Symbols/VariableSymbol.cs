using System;

namespace ReCT.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, bool isGlobal, bool isFunctional, TypeSymbol type, Text.TextLocation location)
            : base(name, location)
        {
            IsReadOnly = isReadOnly;
            IsGlobal = isGlobal;
            IsFunctional = isFunctional;
            Type = type;
        }

        public bool IsReadOnly { get; }
        public bool IsGlobal { get; }
        public bool IsFunctional { get; }
        public TypeSymbol Type { get; }
    }
}