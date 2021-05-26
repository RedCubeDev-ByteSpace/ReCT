using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundArrayLiteralExpression : BoundExpression
    {
        private readonly TypeSymbol _externalType;

        public BoundArrayLiteralExpression(TypeSymbol type, TypeSymbol externalType, BoundExpression[] values)
        {
            ArrayType = type;
            _externalType = externalType;
            Values = values;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayLiteralExpression;
        public override TypeSymbol Type => _externalType;

        public TypeSymbol ArrayType { get; }
        public BoundExpression[] Values { get; }
    }
}
