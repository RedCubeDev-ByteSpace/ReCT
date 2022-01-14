namespace ReCT.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        VariableDeclaration,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        ForStatement,
        LabelStatement,
        GotoStatement,
        ConditionalGotoStatement,
        ReturnStatement,
        ExpressionStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ConversionExpression,
        FromToStatement,
        ObjectAccessExpression,
        ThreadCreateExpression,
        ArrayCreationExpression,
        TryCatchStatement,
        ObjectCreationExpression,
        EnumStatement,
        TernaryExpression,
        ArrayLiteralExpression,
        ActionCreateExpression,
        BaseStatement,
        LambdaExpression,
        IsExpression,
    }
}
