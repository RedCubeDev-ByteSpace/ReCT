using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    public sealed class BoundFromToStatement : BoundLoopStatement
    {
        public BoundFromToStatement(VariableSymbol variable, BoundExpression lowerBound, BoundExpression upperBound, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Body = body;
        }

        public override BoundNodeKind Kind => BoundNodeKind.FromToStatement;
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundStatement Body { get; }
    }
}
