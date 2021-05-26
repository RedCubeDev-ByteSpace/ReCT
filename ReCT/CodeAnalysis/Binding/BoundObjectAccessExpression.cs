using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundObjectAccessExpression : BoundExpression
    {

        public BoundObjectAccessExpression(VariableSymbol variable, ObjectAccessExpression.AccessType accessType, FunctionSymbol function, ImmutableArray<BoundExpression> arguments, VariableSymbol property, TypeSymbol type, BoundExpression value, Package.Package package, ClassSymbol _class, BoundTypeCallExpression typeCall, BoundExpression expression, TypeSymbol innerType)
        {
            Variable = variable;
            AccessType = accessType;
            Function = function;
            Arguments = arguments;
            Property = property;
            _type = type;
            Value = value;
            Package = package;
            Class = _class;
            TypeCall = typeCall;
            Expression = expression;
            InnerType = innerType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ObjectAccessExpression;


        public VariableSymbol Variable;
        public ObjectAccessExpression.AccessType AccessType;
        public FunctionSymbol Function;
        public ImmutableArray<BoundExpression> Arguments;
        public VariableSymbol Property;
        public BoundExpression Value;
        public Package.Package Package;
        public ClassSymbol Class;
        public EnumSymbol Enum;
        public string EnumMember;
        public BoundTypeCallExpression TypeCall;
        public BoundExpression Expression;

        public TypeSymbol _type;
        public override TypeSymbol Type => _type;

        public TypeSymbol InnerType { get; }
    }
}
