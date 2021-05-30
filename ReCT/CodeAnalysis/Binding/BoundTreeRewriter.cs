using System;
using System.Collections.Immutable;

namespace ReCT.CodeAnalysis.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            if (node == null)
                return null;

            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)node);
                case BoundNodeKind.VariableDeclaration:
                    return RewriteVariableDeclaration((BoundVariableDeclaration)node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)node);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement)node);
                case BoundNodeKind.DoWhileStatement:
                    return RewriteDoWhileStatement((BoundDoWhileStatement)node);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement)node);
                case BoundNodeKind.FromToStatement:
                    return RewriteFromToStatement((BoundFromToStatement)node);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement)node);
                case BoundNodeKind.GotoStatement:
                    return RewriteGotoStatement((BoundGotoStatement)node);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node);
                case BoundNodeKind.ReturnStatement:
                    return RewriteReturnStatement((BoundReturnStatement)node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)node);
                case BoundNodeKind.TryCatchStatement:
                    return RewriteTryCatchStatement((BoundTryCatchStatement)node);
                case BoundNodeKind.BaseStatement:
                    return RewriteBaseStatement((BoundBaseStatement)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundStatement RewriteBaseStatement(BoundBaseStatement node)
        {
            ImmutableArray<BoundExpression>.Builder builder = null;

            for (var i = 0; i< node.Arguments.Length; i++)
            {
                var oldArgument = node.Arguments[i];
                var newArgument = RewriteExpression(oldArgument);
                if (newArgument != oldArgument)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);

                        for (var j = 0; j < i; j++)
                            builder.Add(node.Arguments[j]);
                    }
                }

                if (builder != null)
                    builder.Add(newArgument);
            }

            if (builder == null)
                return node;

            return new BoundBaseStatement(builder.ToImmutable());
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder builder = null;

            for (var i = 0; i< node.Statements.Length; i++)
            {
                var oldStatement = node.Statements[i];
                var newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);

                        for (var j = 0; j < i; j++)
                            builder.Add(node.Statements[j]);
                    }
                }

                if (builder != null)
                    builder.Add(newStatement);
            }

            if (builder == null)
                return node;

            return new BoundBlockStatement(builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclaration node)
        {
            if (node.Initializer == null) return node;

            var initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
                return node;

            return new BoundVariableDeclaration(node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var thenStatement = RewriteStatement(node.ThenStatement);
            var elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
            if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
                return node;

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteTryCatchStatement(BoundTryCatchStatement node)
        {
            var normalStatement = RewriteStatement(node.NormalStatement);
            var exceptionStatement = RewriteStatement(node.ExceptionStatement);
            if (normalStatement == node.NormalStatement && exceptionStatement == node.ExceptionStatement)
                return node;

            return new BoundTryCatchStatement(normalStatement, exceptionStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            var body = RewriteStatement(node.Body);
            var condition = RewriteExpression(node.Condition);
            if (body == node.Body && condition == node.Condition)
                return node;

            return new BoundDoWhileStatement(body, condition, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var variable = RewriteVariableDeclaration((BoundVariableDeclaration)node.Variable);
            var condition = RewriteExpression(node.Condition);
            var action = RewriteExpression(node.Action);
            var body = RewriteStatement(node.Body);
            if (variable == node.Variable && condition == node.Condition && action == node.Action && body == node.Body)
                return node;

            return new BoundForStatement(node.Variable, condition, action, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteFromToStatement(BoundFromToStatement node)
        {
            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteStatement(node.Body);
            if (lowerBound == node.LowerBound && upperBound == node.UpperBound && body == node.Body)
                return node;

            return new BoundFromToStatement(node.Variable, lowerBound, upperBound, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            var expression = node.Expression == null ? null : RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundReturnStatement(expression);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(expression);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression)node);
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression)node);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression)node);
                case BoundNodeKind.ObjectAccessExpression:
                    return RewriteRemoteNameExpression((BoundObjectAccessExpression)node);
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression)node);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression)node);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression)node);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression)node);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression)node);
                case BoundNodeKind.ThreadCreateExpression:
                    return RewriteThreadCreateExpression((BoundThreadCreateExpression)node);
                case BoundNodeKind.ActionCreateExpression:
                    return RewriteActionCreateExpression((BoundActionCreateExpression)node);
                case BoundNodeKind.ArrayCreationExpression:
                    return RewriteArrayCreateExpression((BoundArrayCreationExpression)node);
                 case BoundNodeKind.ArrayLiteralExpression:
                    return RewriteArrayLiteralExpression((BoundArrayLiteralExpression)node);
                case BoundNodeKind.ObjectCreationExpression:
                    return RewriteObjectCreateExpression((BoundObjectCreationExpression)node);
                case BoundNodeKind.TernaryExpression:
                    return RewriteTernaryExpression((BoundTernaryExpression)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundExpression RewriteTernaryExpression(BoundTernaryExpression node)
        {
            var condition = RewriteExpression(node.Condition);
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            if (condition == node.Condition && left == node.Left && right == node.Right)
                return node;

            return new BoundTernaryExpression(condition, left, right);
        }

        protected virtual BoundExpression RewriteArrayLiteralExpression(BoundArrayLiteralExpression node)
        {
            var change = false;
            var values = node.Values;

            for (int i = 0; i < values.Length; i++)
            {
                var newval = RewriteExpression(values[i]);

                if (newval != values[i])
                {
                    change = true;
                    values[i] = newval;
                }
            }

            if (!change)
                return node;

            return new BoundArrayLiteralExpression(node.ArrayType, node.Type, values);
        }

        private BoundExpression RewriteObjectCreateExpression(BoundObjectCreationExpression node)
        {
            return node;
        }

        private BoundExpression RewriteArrayCreateExpression(BoundArrayCreationExpression node)
        {
            return node;
        }

        private BoundExpression RewriteThreadCreateExpression(BoundThreadCreateExpression node)
        {
            return node;
        }

        private BoundExpression RewriteActionCreateExpression(BoundActionCreateExpression node)
        {
            return node;
        }

        private BoundExpression RewriteRemoteNameExpression(BoundObjectAccessExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Variable, expression);
        }
        

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var operand = RewriteExpression(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);
            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            ImmutableArray<BoundExpression>.Builder builder = null;

            for (var i = 0; i< node.Arguments.Length; i++)
            {
                var oldArgument = node.Arguments[i];
                var newArgument = RewriteExpression(oldArgument);
                if (newArgument != oldArgument)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);

                        for (var j = 0; j < i; j++)
                            builder.Add(node.Arguments[j]);
                    }
                }

                if (builder != null)
                    builder.Add(newArgument);
            }

            if (builder == null)
                return node;

            return new BoundCallExpression(node.Function, builder.MoveToImmutable(), node.Namespace);
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundConversionExpression(node.Type, expression);
        }
    }
}
