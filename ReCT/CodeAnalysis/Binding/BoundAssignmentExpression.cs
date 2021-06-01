using System;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public BoundAssignmentExpression(VariableSymbol variable, BoundExpression expression, ClassSymbol inClass = null)
        {
            Variable = variable;
            Expression = expression;
            InClass = inClass;
        }
        public BoundAssignmentExpression(VariableSymbol variable, BoundExpression expression, BoundExpression index, ClassSymbol inClass = null)
        {
            Variable = variable;
            Expression = expression;
            Index = index;
            InClass = inClass;
            isArray = true;
        }

        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override TypeSymbol Type => Expression.Type;
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public BoundExpression Index { get; }
        public ClassSymbol InClass { get; }
        public bool isArray { get; }
    }
}
