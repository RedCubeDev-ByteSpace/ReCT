using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ClassDeclarationSyntax : MemberSyntax
    {
        public ClassDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken classKeyword, SyntaxToken identifier, SyntaxToken inheritance, ImmutableArray<MemberSyntax> members, bool isStatic, bool isIncluded, bool isAbstract, bool isSerializable)
            : base(syntaxTree)
        {
            ClassKeyword = classKeyword;
            Identifier = identifier;
            Inheritance = inheritance;
            Members = members;
            IsAbstract = isAbstract;
            IsSerializable = isSerializable;
            IsStatic = isStatic;
            IsIncluded = isIncluded;
        }

        public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;

        public SyntaxToken ClassKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken Inheritance { get; }
        public ImmutableArray<MemberSyntax> Members { get; }
        public bool IsAbstract { get; }
        public bool IsSerializable { get; }
        public bool IsStatic { get; }
        public bool IsIncluded { get; }
    }
}