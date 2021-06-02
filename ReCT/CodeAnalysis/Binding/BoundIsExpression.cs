using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundIsExpression : BoundExpression
    {
        public BoundIsExpression(BoundExpression left, TypeSymbol _class)
        {
            Left = left;
            Class = _class;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IsExpression;


        public override TypeSymbol Type => TypeSymbol.Bool;

        public BoundExpression Left { get; }
        public TypeSymbol Class { get; }
    }
}
