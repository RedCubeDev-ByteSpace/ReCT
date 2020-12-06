namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ObjectCreationSyntax : ExpressionSyntax
    {
        public ObjectCreationSyntax(SyntaxTree syntaxTree, SyntaxToken type, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken package)
            : base(syntaxTree)
        {
            Type = type;
            Arguments = arguments;
            Package = package;
        }

        public override SyntaxKind Kind => SyntaxKind.ObjectCreateExpression;

        public SyntaxToken Type { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken Package { get; }
    }
}