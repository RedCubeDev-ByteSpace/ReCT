using ReCT.CodeAnalysis.Binding;
using ReCT.CodeAnalysis.Syntax;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, ClassDeclarationSyntax declaration = null, bool isStatic = false, bool isIncluded = false)
            : base(name)
        {
            Declaration = declaration;
            IsStatic = isStatic;
            IsIncluded = isIncluded;
        }

        public ClassDeclarationSyntax Declaration { get; }
        public bool IsStatic { get; }
        public bool IsIncluded { get; }

        public object[] Statements;
        public Binding.BoundScope Scope;

        public bool hasConstructor;

        public override SymbolKind Kind => SymbolKind.Class;
    }
}