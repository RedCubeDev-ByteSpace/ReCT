﻿using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundObjectAccessExpression : BoundExpression
    {
        private readonly TypeSymbol _type;

        public BoundObjectAccessExpression(VariableSymbol variable, ObjectAccessExpression.AccessType accessType, FunctionSymbol function, ImmutableArray<BoundExpression> arguments, VariableSymbol property, TypeSymbol type, BoundExpression value, Package.Package package, ClassSymbol _class, BoundCallExpression typeCall, BoundExpression expression)
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
        }

        public override BoundNodeKind Kind => BoundNodeKind.ObjectAccessExpression;
        public VariableSymbol Variable { get; }
        public ObjectAccessExpression.AccessType AccessType { get; }
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public VariableSymbol Property { get; }
        public BoundExpression Value { get; }
        public Package.Package Package { get; }
        public ClassSymbol Class { get; }
        public BoundCallExpression TypeCall { get; }
        public BoundExpression Expression { get; }

        public override TypeSymbol Type => _type;
    }
}
