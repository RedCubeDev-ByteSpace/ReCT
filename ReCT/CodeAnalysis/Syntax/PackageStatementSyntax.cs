namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class PackageStatementSyntax : StatementSyntax
    {
        public PackageStatementSyntax(SyntaxTree syntaxTree, SyntaxToken packageKeyword, SyntaxToken package)
            : base(syntaxTree)
        {
            PackageKeyword = packageKeyword;
            Package = package;
        }

        public override SyntaxKind Kind => SyntaxKind.PackageStatement;

        public SyntaxToken PackageKeyword { get; }
        public SyntaxToken Package { get; }
    }
}