namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ActionCreationSyntax : ExpressionSyntax
    {
        public ActionCreationSyntax(SyntaxTree syntaxTree, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.ActionCreateExpression;
        public SyntaxToken Identifier { get; }
    }
}