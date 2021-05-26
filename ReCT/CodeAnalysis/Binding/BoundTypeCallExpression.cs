using System.Collections.Immutable;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundTypeCallExpression : BoundExpression
    {
        public BoundTypeCallExpression(TypeFunctionSymbol function, ImmutableArray<BoundExpression> arguments, string @namespace, TypeSymbol retType)
        {
            Function = function;
            Arguments = arguments;
            Namespace = @namespace;
            Type = retType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type { get; }
        public TypeFunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public string Namespace { get; }
    }
}
