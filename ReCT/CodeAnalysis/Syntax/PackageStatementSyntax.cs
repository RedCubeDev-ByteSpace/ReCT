namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class PackageStatementSyntax : StatementSyntax
    {
        public PackageStatementSyntax(SyntaxTree syntaxTree, SyntaxToken packageKeyword, SyntaxToken package, bool isDll)
            : base(syntaxTree)
        {
            PackageKeyword = packageKeyword;
            Package = package;
            IsDll = isDll;
        }

        public override SyntaxKind Kind => SyntaxKind.PackageStatement;

        public SyntaxToken PackageKeyword { get; }
        public SyntaxToken Package { get; }
        public bool IsDll { get; }
    }
}