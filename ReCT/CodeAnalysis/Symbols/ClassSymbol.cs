using ReCT.CodeAnalysis.Syntax;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, ClassDeclarationSyntax declaration = null, bool isStatic = false)
            : base(name)
        {
            Declaration = declaration;
            IsStatic = isStatic;
        }

        public ClassDeclarationSyntax Declaration { get; }
        public bool IsStatic { get; }

        public override SymbolKind Kind => SymbolKind.Class;
    }
}