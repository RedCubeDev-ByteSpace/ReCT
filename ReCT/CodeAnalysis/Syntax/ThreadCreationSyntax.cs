namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ThreadCreationSyntax : ExpressionSyntax
    {
        public ThreadCreationSyntax(SyntaxTree syntaxTree, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.ThreadCreateExpression;
        public SyntaxToken Identifier { get; }
    }
}