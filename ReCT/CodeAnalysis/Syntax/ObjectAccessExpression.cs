namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class ObjectAccessExpression : ExpressionSyntax
    {
        public ObjectAccessExpression(SyntaxTree syntaxTree, SyntaxToken identifierToken, AccessType type, CallExpressionSyntax call, SyntaxToken lookingFor, ExpressionSyntax value)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            Type = type;
            Call = call;
            LookingFor = lookingFor;
            Value = value;
        }

        public override SyntaxKind Kind => SyntaxKind.ObjectAccessExpression;
        public SyntaxToken IdentifierToken { get; }
        public AccessType Type { get; }
        public CallExpressionSyntax Call { get; }
        public SyntaxToken LookingFor { get; }
        public ExpressionSyntax Value { get; }

        public enum AccessType
        {
            Call,
            Get,
            Set,
        }
    }
}