using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Syntax
{
    public sealed class VariableDeclarationSyntax : StatementSyntax
    {
        public VariableDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken identifier, TypeClauseSyntax typeClause, SyntaxToken equalsToken, ExpressionSyntax initializer, TypeSymbol externalType, bool isVirtual, bool isOverride)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Identifier = identifier;
            TypeClause = typeClause;
            EqualsToken = equalsToken;
            Initializer = initializer;
            ExternalType = externalType;
            IsVirtual = isVirtual;
            IsOverride = isOverride;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;
        public SyntaxToken Keyword { get; }
        public SyntaxToken Identifier { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Initializer { get; }
        public TypeSymbol ExternalType { get; }
        public bool IsVirtual { get; }
        public bool IsOverride { get; }
    }
}