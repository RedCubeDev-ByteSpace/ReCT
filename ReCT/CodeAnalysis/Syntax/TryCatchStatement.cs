namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class TryCatchStatementSyntax : StatementSyntax
    {
        public TryCatchStatementSyntax(SyntaxTree syntaxTree, SyntaxToken tryKeyword, StatementSyntax normalStatement, SyntaxToken catchKeyword, StatementSyntax exceptionSyntax)
            : base(syntaxTree)
        {
            TryKeyword = tryKeyword;
            NormalStatement = normalStatement;
            CatchKeyword = catchKeyword;
            ExceptionSyntax = exceptionSyntax;
        }

        public override SyntaxKind Kind => SyntaxKind.TryCatchStatement;

        public SyntaxToken TryKeyword { get; }
        public StatementSyntax NormalStatement { get; }
        public SyntaxToken CatchKeyword { get; }
        public StatementSyntax ExceptionSyntax { get; }
    }
}