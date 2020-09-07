using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundRemoteNameExpression : BoundExpression
    {
        public BoundRemoteNameExpression(VariableSymbol variable, BoundCallExpression call)
        {
            Variable = variable;
            Call = call;
        }

        public override BoundNodeKind Kind => BoundNodeKind.RemoteNameExpression;
        public override TypeSymbol Type => Call.Type;
        public VariableSymbol Variable { get; }
        public BoundCallExpression Call { get; }
    }
}
