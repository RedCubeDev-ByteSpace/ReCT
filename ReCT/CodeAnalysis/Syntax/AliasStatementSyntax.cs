namespace ReCT.CodeAnalysis.Syntax
{
    internal class AliasStatementSyntax : StatementSyntax
    {
        public AliasStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken mapThis, SyntaxToken toThis)
            : base(syntaxTree)
        {
            Keyword = keyword;
            MapThis = mapThis;
            ToThis = toThis;
        }

        public override SyntaxKind Kind => SyntaxKind.AliasStatement;
        public SyntaxToken Keyword { get; }
        public SyntaxToken MapThis { get; }
        public SyntaxToken ToThis { get; }
    }
}