namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundTryCatchStatement : BoundStatement
    {
        public BoundTryCatchStatement(BoundBlockStatement normalStatement, BoundBlockStatement exceptionStatement)
        {
            NormalStatement = normalStatement;
            ExceptionStatement = exceptionStatement;
        }

        public override BoundNodeKind Kind => BoundNodeKind.TryCatchStatement;
        public BoundBlockStatement NormalStatement { get; }
        public BoundBlockStatement ExceptionStatement { get; }
    }
}
