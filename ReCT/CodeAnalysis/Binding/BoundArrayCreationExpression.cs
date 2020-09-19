using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundArrayCreationExpression : BoundExpression
    {
        private readonly TypeSymbol _externalType;

        public BoundArrayCreationExpression(TypeSymbol type, BoundExpression length, TypeSymbol externalType)
        {
            ArrayType = type;
            Length = length;
            _externalType = externalType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayCreationExpression;
        public override TypeSymbol Type => _externalType;

        public TypeSymbol ArrayType { get; }
        public BoundExpression Length { get; }
    }
}
