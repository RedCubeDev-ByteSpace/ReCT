namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class LambdaExpressionSyntax : ExpressionSyntax
    {
        public LambdaExpressionSyntax(SyntaxTree syntaxTree, StatementSyntax block)
            : base(syntaxTree)
        {
            Block = block;
        }

        public override SyntaxKind Kind => SyntaxKind.LambdaExpression;

        public StatementSyntax Block { get; }
    }
}