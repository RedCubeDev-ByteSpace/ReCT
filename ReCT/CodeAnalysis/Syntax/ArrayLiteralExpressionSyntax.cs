namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ArrayLiteralExpressionSyntax : ExpressionSyntax
    {
        public ArrayLiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken type, SyntaxToken package, ExpressionSyntax[] values)
            : base(syntaxTree)
        {
            Type = type;
            Package = package;
            Values = values;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayLiteralExpression;

        public SyntaxToken Type { get; }
        public ExpressionSyntax Length { get; }
        public SyntaxToken Package { get; }
        public ExpressionSyntax[] Values { get; }
    }
}