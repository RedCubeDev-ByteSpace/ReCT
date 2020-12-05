using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ClassDeclarationSyntax : MemberSyntax
    {
        public ClassDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken classKeyword, SyntaxToken identifier, ImmutableArray<MemberSyntax> members, bool _static)
            : base(syntaxTree)
        {
            ClassKeyword = classKeyword;
            Identifier = identifier;
            Members = members;
            isStatic = _static;
        }

        public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;

        public SyntaxToken ClassKeyword { get; }
        public SyntaxToken Identifier { get; }
        public ImmutableArray<MemberSyntax> Members { get; }
        public bool isStatic { get; }
    }
}