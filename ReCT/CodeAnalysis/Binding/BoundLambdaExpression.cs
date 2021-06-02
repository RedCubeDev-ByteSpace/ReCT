using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundLambdaExpression : BoundExpression
    {
        public BoundLambdaExpression(BoundStatement block)
        {
            Block = block;
        }

        public override BoundNodeKind Kind => BoundNodeKind.LambdaExpression;

        public BoundStatement Block { get; }

        public override TypeSymbol Type => TypeSymbol.Action;
    }
}
