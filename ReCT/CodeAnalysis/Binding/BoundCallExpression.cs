using System.Collections.Immutable;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundCallExpression(FunctionSymbol function, ImmutableArray<BoundExpression> arguments, string @namespace, ClassSymbol inClass = null)
        {
            Function = function;
            Arguments = arguments;
            Namespace = @namespace;
            InClass = inClass;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.Type;
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public string Namespace { get; }
        public ClassSymbol InClass { get; }
    }
}
