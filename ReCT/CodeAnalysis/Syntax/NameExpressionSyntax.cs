using ReCT.CodeAnalysis.Symbols;

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