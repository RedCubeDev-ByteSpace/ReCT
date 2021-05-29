using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundBaseStatement : BoundStatement
    {
        public BoundBaseStatement(ImmutableArray<BoundExpression> arguments)
        {
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BaseStatement;

        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
