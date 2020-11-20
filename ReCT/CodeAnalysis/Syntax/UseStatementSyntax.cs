namespace ReCT.CodeAnalysis.Syntax
{
    internal class UseStatementSyntax : StatementSyntax
    {
        public UseStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken name)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
        }

        public override SyntaxKind Kind => SyntaxKind.UseStatement;
        public SyntaxToken Keyword { get; }
        public SyntaxToken Name { get; }
    }
}