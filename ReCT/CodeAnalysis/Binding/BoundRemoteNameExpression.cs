using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundRemoteNameExpression : BoundExpression
    {
        public BoundRemoteNameExpression(VariableSymbol variable, string callName)
        {
            Variable = variable;
            CallName = callName;
        }

        public override BoundNodeKind Kind => BoundNodeKind.RemoteNameExpression;
        public override TypeSymbol Type => Variable.Type;
        public VariableSymbol Variable { get; }
        public string CallName { get; }
    }
}
