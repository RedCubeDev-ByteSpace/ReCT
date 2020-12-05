using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundObjectAccessExpression : BoundExpression
    {
        private readonly TypeSymbol _type;

        public BoundObjectAccessExpression(VariableSymbol variable, ObjectAccessExpression.AccessType accessType, FunctionSymbol function, ImmutableArray<BoundExpression> arguments, VariableSymbol property, TypeSymbol type, BoundExpression value)
        {
            Variable = variable;
            AccessType = accessType;
            Function = function;
            Arguments = arguments;
            Property = property;
            _type = type;
            Value = value;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ObjectAccessExpression;
        public VariableSymbol Variable { get; }
        public ObjectAccessExpression.AccessType AccessType { get; }
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public VariableSymbol Property { get; }
        public BoundExpression Value { get; }

        public override TypeSymbol Type => _type;
    }
}
