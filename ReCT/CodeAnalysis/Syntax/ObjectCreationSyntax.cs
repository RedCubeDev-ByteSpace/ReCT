namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ObjectCreationSyntax : ExpressionSyntax
    {
        public ObjectCreationSyntax(SyntaxTree syntaxTree, SyntaxToken type, SeparatedSyntaxList<ExpressionSyntax> arguments)
            : base(syntaxTree)
        {
            Type = type;
            Arguments = arguments;
        }

        public override SyntaxKind Kind => SyntaxKind.ObjectCreateExpression;

        public SyntaxToken Type { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
    }
}