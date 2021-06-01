using System;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public BoundVariableExpression(VariableSymbol variable, ClassSymbol inClass = null)
        {
            Variable = variable;
            InClass = inClass;
            Type = Variable.Type;
        }

        public BoundVariableExpression(VariableSymbol variable, BoundExpression index, TypeSymbol baseType, ClassSymbol inClass = null)
        {
            Variable = variable;
            Index = index;
            isArray = true;
            Type = baseType;
            InClass = inClass;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public override TypeSymbol Type { get; }
        public VariableSymbol Variable { get; }
        public ClassSymbol InClass { get; }
        public BoundExpression Index { get; }
        public bool isArray { get; }
    }
}
