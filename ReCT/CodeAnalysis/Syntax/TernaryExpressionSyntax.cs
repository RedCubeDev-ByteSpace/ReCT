namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class TernaryExpressionSyntax : ExpressionSyntax
    {
        public TernaryExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax condition, ExpressionSyntax left, ExpressionSyntax right)
            : base(syntaxTree)
        {
            Condition = condition;
            Left = left;
            Right = right;
        }

        public override SyntaxKind Kind => SyntaxKind.TernaryExpression;
        public ExpressionSyntax Condition { get; }
        public ExpressionSyntax Left { get; }
        public ExpressionSyntax Right { get; }
    }
}