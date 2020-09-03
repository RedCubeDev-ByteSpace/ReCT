namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ForStatementSyntax : StatementSyntax
    {
        public ForStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, StatementSyntax variable, ExpressionSyntax condition, ExpressionSyntax action, StatementSyntax body)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Variable = variable;
            Condition = condition;
            Action = action;
            Body = body;
        }

        public override SyntaxKind Kind => SyntaxKind.ForStatement;
        public SyntaxToken Keyword { get; }
        public StatementSyntax Variable { get; }
        public ExpressionSyntax Condition { get; }
        public ExpressionSyntax Action { get; }
        public StatementSyntax Body { get; }
    }
}