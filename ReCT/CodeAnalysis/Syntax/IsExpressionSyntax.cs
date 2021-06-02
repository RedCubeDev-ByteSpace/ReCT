namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class IsExpressionSyntax : ExpressionSyntax
    {
        public IsExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax left, SyntaxToken type)
            : base(syntaxTree)
        {
            Left = left;
            Type = type;
        }

        public override SyntaxKind Kind => SyntaxKind.IsExpression;

        public ExpressionSyntax Left { get; }
        public SyntaxToken Type { get; }
    }
}