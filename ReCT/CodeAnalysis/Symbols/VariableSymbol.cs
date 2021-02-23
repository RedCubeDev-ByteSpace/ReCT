using System;

namespace ReCT.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, bool isGlobal, bool isFunctional, TypeSymbol type)
            : base(name)
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