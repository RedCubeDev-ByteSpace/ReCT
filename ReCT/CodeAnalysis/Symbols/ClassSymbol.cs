using ReCT.CodeAnalysis.Binding;
using ReCT.CodeAnalysis.Syntax;

namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, ClassDeclarationSyntax declaration = null, bool isStatic = false, bool isIncluded = false, bool isAbstract = false, bool isSerializable = false, ClassSymbol parentSym = null)
            : base(name)
        {
            Declaration = declaration;
            IsStatic = isStatic;
            IsIncluded = isIncluded;
            IsAbstract = isAbstract;
            IsSerializable = isSerializable;
            ParentSym = parentSym;
        }

        public ClassDeclarationSyntax Declaration { get; }
        public bool IsStatic { get; }
        public bool IsIncluded { get; }
        public bool IsAbstract { get; }
        public bool IsSerializable { get; }
        public ClassSymbol ParentSym { get; }

        public object[] Statements;
        public Binding.BoundScope Scope;

        public bool hasConstructor;

        public override SymbolKind Kind => SymbolKind.Class;
    }
}