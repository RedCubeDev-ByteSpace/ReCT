using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundActionCreateExpression : BoundExpression
    {
        public BoundActionCreateExpression(FunctionSymbol function)
        {
            Function = function;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ActionCreateExpression;
        public override TypeSymbol Type => TypeSymbol.Action;
        public FunctionSymbol Function { get; }
    }
}
