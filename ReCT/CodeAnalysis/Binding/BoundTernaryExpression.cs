using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundTernaryExpression : BoundExpression
    {
        public BoundTernaryExpression(BoundExpression condition, BoundExpression left, BoundExpression right)
        {
            Condition = condition;
            Left = left;
            Right = right;
        }

        public override BoundNodeKind Kind => BoundNodeKind.TernaryExpression;
        public override TypeSymbol Type => Left.Type;
        public BoundExpression Condition { get; }
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
    }
}
