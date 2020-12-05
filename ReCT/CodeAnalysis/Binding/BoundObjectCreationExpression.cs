using System.Collections.Immutable;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundObjectCreationExpression : BoundExpression
    {
        public BoundObjectCreationExpression(ClassSymbol _class, ImmutableArray<BoundExpression> arguments)
        {
            Class = _class;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ObjectCreationExpression;
        public override TypeSymbol Type => TypeSymbol.Class[Class];

        public ClassSymbol Class { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
