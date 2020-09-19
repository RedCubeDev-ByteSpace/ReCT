namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ArrayCreationSyntax : ExpressionSyntax
    {
        public ArrayCreationSyntax(SyntaxTree syntaxTree, SyntaxToken type, ExpressionSyntax length)
            : base(syntaxTree)
        {
            Type = type;
            Length = length;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayCreateExpression;

        public SyntaxToken Type { get; }
        public ExpressionSyntax Length { get; }
    }
}