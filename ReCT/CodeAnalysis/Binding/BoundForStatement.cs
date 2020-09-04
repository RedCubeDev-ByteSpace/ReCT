namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundForStatement : BoundLoopStatement
    {
        public BoundForStatement(BoundStatement variable, BoundExpression condition, BoundExpression action, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Variable = variable;
            Condition = condition;
            Action = action;
            Body = body;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;
        public BoundStatement Variable { get; }
        public BoundExpression Condition { get; }
        public BoundExpression Action { get; }
        public BoundStatement Body { get; }
    }
}
