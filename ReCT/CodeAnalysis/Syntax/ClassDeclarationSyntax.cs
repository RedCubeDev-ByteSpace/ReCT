namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ClassDeclarationSyntax : MemberSyntax
    {
        public ClassDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken classKeyword, SyntaxToken identifier, BlockStatementSyntax body, bool _static)
            : base(syntaxTree)
        {
            ClassKeyword = classKeyword;
            Identifier = identifier;
            Body = body;
            isStatic = _static;
        }

        public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;

        public SyntaxToken ClassKeyword { get; }
        public SyntaxToken Identifier { get; }
        public BlockStatementSyntax Body { get; }
        public bool isStatic { get; }
    }
}