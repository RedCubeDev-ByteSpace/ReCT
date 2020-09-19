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

        public NameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, ExpressionSyntax index)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            Index = index;
            isArray = true;
        }

        public override SyntaxKind Kind => SyntaxKind.NameExpression;
        public SyntaxToken IdentifierToken { get; }
        public ExpressionSyntax Index { get; }
        public bool isArray { get; }
    }
}