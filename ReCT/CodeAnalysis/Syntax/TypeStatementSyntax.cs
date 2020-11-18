namespace ReCT.CodeAnalysis.Syntax
{
    internal class TypeStatementSyntax : StatementSyntax
    {
        public TypeStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken name)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
        }

        public override SyntaxKind Kind => SyntaxKind.TypeStatement;
        public SyntaxToken Keyword { get; }
        public SyntaxToken Name { get; }
    }
}