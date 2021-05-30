using System.Collections.Generic;

namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class EnumDeclarationSyntax : MemberSyntax
    {
        public EnumDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken enumName, SyntaxToken[] names, Dictionary<SyntaxToken, ExpressionSyntax> values)
            : base(syntaxTree)
        {
            EnumName = enumName;
            Names = names;
            Values = values;
        }

        public override SyntaxKind Kind => SyntaxKind.EnumStatement;

        public SyntaxToken TryKeyword { get; }
        public StatementSyntax NormalStatement { get; }
        public SyntaxToken CatchKeyword { get; }
        public StatementSyntax ExceptionSyntax { get; }
        public SyntaxToken EnumName { get; }
        public SyntaxToken[] Names { get; }
        public Dictionary<SyntaxToken, ExpressionSyntax> Values { get; }
    }
}