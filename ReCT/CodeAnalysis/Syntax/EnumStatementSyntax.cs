using System.Collections.Generic;

namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class EnumStatementSyntax : StatementSyntax
    {
        public EnumStatementSyntax(SyntaxTree syntaxTree, SyntaxToken enumKeyword, SyntaxToken[] names, Dictionary<SyntaxToken, ExpressionSyntax> values)
            : base(syntaxTree)
        {
            EnumKeyword = enumKeyword;
            Names = names;
            Values = values;
        }

        public override SyntaxKind Kind => SyntaxKind.TryCatchStatement;

        public SyntaxToken TryKeyword { get; }
        public StatementSyntax NormalStatement { get; }
        public SyntaxToken CatchKeyword { get; }
        public StatementSyntax ExceptionSyntax { get; }
        public SyntaxToken EnumKeyword { get; }
        public SyntaxToken[] Names { get; }
        public Dictionary<SyntaxToken, ExpressionSyntax> Values { get; }
    }
}