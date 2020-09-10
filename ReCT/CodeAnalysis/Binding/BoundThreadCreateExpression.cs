using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundThreadCreateExpression : BoundExpression
    {
        public BoundThreadCreateExpression(FunctionSymbol function)
        {
            Function = function;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ThreadCreateExpression;
        public override TypeSymbol Type => TypeSymbol.Thread;
        public FunctionSymbol Function { get; }
    }
}
