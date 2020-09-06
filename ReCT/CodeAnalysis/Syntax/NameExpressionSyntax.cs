namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class NameExpressionSyntax : ExpressionSyntax
    {
        public NameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
        }

        public override SyntaxKind Kind => SyntaxKind.NameExpression;
        public SyntaxToken IdentifierToken { get; }
    }
    public sealed class RemoteNameExpressionSyntax : ExpressionSyntax
    {
        public RemoteNameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, string name)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            Name = name;
        }

        public override SyntaxKind Kind => SyntaxKind.NameExpression;
        public SyntaxToken IdentifierToken { get; }
        public string Name { get; }
    }
}