using System;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
            Type = Variable.Type;
        }

        public BoundVariableExpression(VariableSymbol variable, BoundExpression index, TypeSymbol baseType)
        {
            Variable = variable;
            Index = index;
            isArray = true;
            Type = baseType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public override TypeSymbol Type { get; }
        public VariableSymbol Variable { get; }
        public BoundExpression Index { get; }
        public bool isArray { get; }
    }
}
