namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class TryCatchStatementSyntax : StatementSyntax
    {
        public TryCatchStatementSyntax(SyntaxTree syntaxTree, SyntaxToken tryKeyword, BlockStatementSyntax normalStatement, SyntaxToken catchKeyword, BlockStatementSyntax exceptionSyntax)
            : base(syntaxTree)
        {
            TryKeyword = tryKeyword;
            NormalStatement = normalStatement;
            CatchKeyword = catchKeyword;
            ExceptionSyntax = exceptionSyntax;
        }

        public override SyntaxKind Kind => SyntaxKind.TryCatchStatement;

        public SyntaxToken TryKeyword { get; }
        public BlockStatementSyntax NormalStatement { get; }
        public SyntaxToken CatchKeyword { get; }
        public BlockStatementSyntax ExceptionSyntax { get; }
    }
}