namespace ReCT.CodeAnalysis.Binding
{
    public sealed class BoundTryCatchStatement : BoundStatement
    {
        public BoundTryCatchStatement(BoundStatement normalStatement, BoundStatement exceptionStatement)
        {
            NormalStatement = normalStatement;
            ExceptionStatement = exceptionStatement;
        }

        public override BoundNodeKind Kind => BoundNodeKind.TryCatchStatement;
        public BoundStatement NormalStatement { get; }
        public BoundStatement ExceptionStatement { get; }
    }
}
