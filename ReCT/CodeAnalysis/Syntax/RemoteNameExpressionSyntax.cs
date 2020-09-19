namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class RemoteNameExpressionSyntax : ExpressionSyntax
    {
        public RemoteNameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, CallExpressionSyntax call)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            Call = call;
        }

        public override SyntaxKind Kind => SyntaxKind.RemoteNameExpression;
        public SyntaxToken IdentifierToken { get; }
        public CallExpressionSyntax Call { get; }
    }
}