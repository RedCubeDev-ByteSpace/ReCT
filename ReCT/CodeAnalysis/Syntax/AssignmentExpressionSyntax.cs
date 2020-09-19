namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression, ExpressionSyntax index)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
            Index = index;
            isArray = true;
        }

        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public ExpressionSyntax Index { get; }
        public bool isArray { get; }
    }
}