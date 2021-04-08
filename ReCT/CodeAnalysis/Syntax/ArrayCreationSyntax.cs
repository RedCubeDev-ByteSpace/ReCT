namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ArrayCreationSyntax : ExpressionSyntax
    {
        public ArrayCreationSyntax(SyntaxTree syntaxTree, SyntaxToken type, ExpressionSyntax length, SyntaxToken package)
            : base(syntaxTree)
        {
            Type = type;
            Length = length;
            Package = package;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayCreateExpression;

        public SyntaxToken Type { get; }
        public ExpressionSyntax Length { get; }
        public SyntaxToken Package { get; }
    }
}