namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class BaseStatementSyntax : StatementSyntax
    {
        public BaseStatementSyntax(SyntaxTree syntaxTree, SyntaxToken baseKeyword, SeparatedSyntaxList<ExpressionSyntax> arguments)
            : base(syntaxTree)
        {
            BaseKeyword = baseKeyword;
            Arguments = arguments;
        }

        public override SyntaxKind Kind => SyntaxKind.BaseStatement;

        public SyntaxToken BaseKeyword { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
    }
}