using System;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    public abstract class BoundExpression : BoundNode
    {
        public abstract TypeSymbol Type { get; }
    }
}
