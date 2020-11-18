namespace ReCT.CodeAnalysis.Syntax
{
    internal class NamespaceStatementSyntax : StatementSyntax
    {
        public NamespaceStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken name)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
        }

        public override SyntaxKind Kind => SyntaxKind.NamespaceStatement;
        public SyntaxToken Keyword { get; }
        public SyntaxToken Name { get; }
    }
}