using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ReCT.CodeAnalysis.Lowering;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using ReCT.CodeAnalysis.Text;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public static DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly bool _isScript;
        private readonly FunctionSymbol _function;

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        private int _labelCounter;
        private BoundScope _scope;
        private ClassSymbol inClass = null;
        static BoundScope ParentScope;

        public static List<Package.Package> _packageNamespaces = new List<Package.Package>();
        public static string _namespace = "";
        public static string _type = "";
        public static List<string> _usingPackages = new List<string>();

        public Binder(bool isScript, BoundScope parent, FunctionSymbol function, ClassSymbol inclass = null)
        {
            _scope = new BoundScope(parent);
            _isScript = isScript;
            _function = function;

            inClass = inclass;

            if (function != null)
            {
                foreach (var p in function.Parameters)
                    _scope.TryDeclareVariable(p);
            }
        }

        public static BoundGlobalScope BindGlobalScope(bool isScript, BoundGlobalScope previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            var parentScope = CreateParentScope(previous);
            parentScope.Name = "ProgramScope";
            var binder = new Binder(isScript, parentScope, function: null);
            ParentScope = binder._scope;

            var functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();

            var classDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<ClassDeclarationSyntax>();
            
            var globalStatements = syntaxTrees.SelectMany(st => st.Root.Members)
                .OfType<GlobalStatementSyntax>();
            
            //package imports BEFORE anything else
            foreach (var statement in globalStatements)
            {
                if (statement.Statement is PackageStatementSyntax p)
                {
                    binder.BindGlobalStatement(p);
                    Console.WriteLine("imported: " + p.Package.Text);
                }
                if (statement.Statement is UseStatementSyntax u)
                {
                    binder.BindGlobalStatement(u);
                    Console.WriteLine("using: " + u.Name.Text);
                }
            }

            //bind classes functions and class-functions
            foreach (var _class in classDeclarations)
                binder.BindClassDeclaration(_class);

            foreach (var function in functionDeclarations)
                binder.BindFunctionDeclaration(function);

            foreach (var _class in binder._scope.GetDeclaredClasses())
                binder.BindClassFunctionDeclaration(_class.Declaration, _class);
            

            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (var globalStatement in globalStatements)
            {
                if (globalStatement.Statement is PackageStatementSyntax || globalStatement.Statement is UseStatementSyntax) continue;
                
                var statement = binder.BindGlobalStatement(globalStatement.Statement);
                statements.Add(statement);
            }

            // Check global statements

            var firstGlobalStatementPerSyntaxTree = syntaxTrees.Select(st => st.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault())
                                                                .Where(g => g != null)
                                                                .ToArray();

            if (firstGlobalStatementPerSyntaxTree.Length > 1)
            {
                foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                    binder.Diagnostics.ReportOnlyOneFileCanHaveGlobalStatements(globalStatement.Location);
            }

            // Check for main/script with global statements

            var functions = binder._scope.GetDeclaredFunctions();

            FunctionSymbol mainFunction;
            FunctionSymbol scriptFunction;

            if (isScript)
            {
                mainFunction = null;
                if (globalStatements.Any())
                {
                    scriptFunction = new FunctionSymbol("$eval", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Any, null);
                }
                else
                {
                    scriptFunction = null;
                }
            }
            else
            {
                mainFunction = functions.FirstOrDefault(f => f.Name == "main");
                scriptFunction = null;

                if (mainFunction != null)
                {
                    if (mainFunction.Type != TypeSymbol.Void || mainFunction.Parameters.Any())
                        binder.Diagnostics.ReportMainMustHaveCorrectSignature(mainFunction.Declaration.Identifier.Location);
                }

                if (globalStatements.Any())
                {
                    if (mainFunction != null)
                    {
                        binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(mainFunction.Declaration.Identifier.Location);

                        foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                            binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(globalStatement.Location);
                    }
                    else
                    {
                        mainFunction = new FunctionSymbol("main", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null);
                    }
                }
            }

            var diagnostics = binder.Diagnostics.ToImmutableArray();
            var variables = binder._scope.GetDeclaredVariables();
            var classes = binder._scope.GetDeclaredClasses();

            if (previous != null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, mainFunction, scriptFunction, functions, variables, statements.ToImmutable(), classes);
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram previous, BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            var classBodies = ImmutableDictionary.CreateBuilder<ClassSymbol, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            //_packageNamespaces = new List<Package.Package>();

            foreach (var _class in globalScope.Classes)
            {
                var cFunctionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
                _class.Scope.ClearVariables();
                //Console.WriteLine("CLASS SCOPE: " + _class.Name);

                var symbol = _class.Scope.TryLookupSymbol("Constructor");
                if (symbol != null && symbol is FunctionSymbol fs)
                {
                    var binder = new Binder(isScript, _class.Scope, fs, _class);
                    var body = binder.BindStatement(fs.Declaration.Body);
                    var loweredBody = Lowerer.Lower(fs, body);

                    if (fs.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                        Binder._diagnostics.ReportAllPathsMustReturn(fs.Declaration.Identifier.Location);

                    cFunctionBodies.Add(fs, loweredBody);
                }

                foreach (var function in _class.Scope.GetDeclaredFunctions())
                {
                    //Console.WriteLine("BINDING: " + function.Name);
                    if (function.Name == "Constructor") continue;

                    var binder = new Binder(isScript, _class.Scope, function, _class);
                    var body = binder.BindStatement(function.Declaration.Body);
                    var loweredBody = Lowerer.Lower(function, body);

                    if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                        Binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                    cFunctionBodies.Add(function, loweredBody);
                }
                classBodies.Add(_class, cFunctionBodies.ToImmutable());
            }

            foreach (var function in globalScope.Functions)
            {
                var binder = new Binder(isScript, parentScope, function);
                var body = binder.BindStatement(function.Declaration.Body);
                var loweredBody = Lowerer.Lower(function, body);

                if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                    Binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                functionBodies.Add(function, loweredBody);
            }

            diagnostics.AddRange(Binder._diagnostics);

            Binder._diagnostics = new DiagnosticBag();

            if (globalScope.MainFunction != null && globalScope.Statements.Any())
            {
                var body = Lowerer.Lower(globalScope.MainFunction, new BoundBlockStatement(globalScope.Statements));
                functionBodies.Add(globalScope.MainFunction, body);
            }
            else if (globalScope.ScriptFunction != null)
            {
                var statements = globalScope.Statements;
                if (statements.Length == 1 &&
                    statements[0] is BoundExpressionStatement es &&
                    es.Expression.Type != TypeSymbol.Void)
                {
                    statements = statements.SetItem(0, new BoundReturnStatement(es.Expression));
                }
                else if (statements.Any() && statements.Last().Kind != BoundNodeKind.ReturnStatement)
                {
                    var nullValue = new BoundLiteralExpression("");
                    statements = statements.Add(new BoundReturnStatement(nullValue));
                }

                var body = Lowerer.Lower(globalScope.ScriptFunction, new BoundBlockStatement(statements));
                functionBodies.Add(globalScope.ScriptFunction, body);
            }

            return new BoundProgram(previous,
                                    diagnostics.ToImmutable(),
                                    globalScope.MainFunction,
                                    globalScope.ScriptFunction,
                                    functionBodies.ToImmutable(),
                                    classBodies.ToImmutable(),
                                    _packageNamespaces.ToImmutableArray(),
                                    _namespace, _type);
        }

        BoundScope getClassScope()
        {
            if (inClass == null)
                return _scope;

            var currentScope = _scope;

            while(currentScope.Name != inClass.Name)
            {
                currentScope = currentScope.Parent;
            }

            return currentScope;
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            var seenParameterNames = new HashSet<string>();

            foreach (var parameterSyntax in syntax.Parameters)
            {
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeClause(parameterSyntax.Type);
                if (!seenParameterNames.Add(parameterName))
                {
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax, syntax.IsPublic);
            if (function.Declaration.Identifier.Text != null &&
                !_scope.TryDeclareFunction(function))
            {
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
            }
        }

        private void BindClassDeclaration(ClassDeclarationSyntax syntax)
        {
            var classSymbol = new ClassSymbol(syntax.Identifier.Text, syntax, syntax.isStatic);
            classSymbol.Scope = new BoundScope(CreateRootScope(), syntax.Identifier.Text);

            if (classSymbol.Declaration.Identifier.Text != null &&
                !_scope.TryDeclareClass(classSymbol))
            {
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, classSymbol.Name);
            }

            if (TypeSymbol.Class == null) {TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>(); }

            if (!TypeSymbol.Class.ContainsKey(classSymbol))
            {
               // Console.WriteLine("ADDED CLASS: " + classSymbol.Name);
                var classTypeSymbol = new TypeSymbol(classSymbol.Name);
                classTypeSymbol.isClass = true;
                TypeSymbol.Class.Add(classSymbol, classTypeSymbol);

                if (!classSymbol.IsStatic)
                {
                    var classArraySymbol = new TypeSymbol(classSymbol.Name + "Arr");
                    classArraySymbol.isClass = true;
                    classArraySymbol.isClassArray = true;
                    TypeSymbol.Class.Add(new ClassSymbol(classSymbol.Name + "Arr", null, false), classArraySymbol);
                }
            }
        }

        private void BindClassFunctionDeclaration(ClassDeclarationSyntax syntax, ClassSymbol _class)
        {
            foreach (MemberSyntax m in syntax.Members)
            {
                if (m is FunctionDeclarationSyntax fsyntax)
                {
                    var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

                    var seenParameterNames = new HashSet<string>();

                    foreach (var parameterSyntax in fsyntax.Parameters)
                    {
                        var parameterName = parameterSyntax.Identifier.Text;
                        var parameterType = BindTypeClause(parameterSyntax.Type);
                        if (!seenParameterNames.Add(parameterName))
                        {
                            _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                        }
                        else
                        {
                            var parameter = new ParameterSymbol(parameterName, parameterType, parameters.Count);
                            parameters.Add(parameter);
                        }
                    }

                    var type = BindTypeClause(fsyntax.Type) ?? TypeSymbol.Void;

                    var function = new FunctionSymbol(fsyntax.Identifier.Text, parameters.ToImmutable(), type, fsyntax, fsyntax.IsPublic);
                    if (function.Declaration.Identifier.Text != null &&
                        !_class.Scope.TryDeclareFunction(function))
                    {
                        _diagnostics.ReportSymbolAlreadyDeclared(fsyntax.Identifier.Location, function.Name);
                    }

                    if (function.Name == "Constructor")
                    {
                        _diagnostics.Report(default, "BINDING CONSTRUCTOR");
                        var binder = new Binder(false, _class.Scope, function, _class);
                        binder.BindStatement(function.Declaration.Body);

                        _diagnostics.removeRangeBefore("BINDING CONSTRUCTOR");
                    }
                }
            }
        }

        private static BoundScope CreateParentScope(BoundGlobalScope previous)
        {
            var stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var parent = CreateRootScope();

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);

                foreach (var f in previous.Functions)
                    scope.TryDeclareFunction(f);

                foreach (var v in previous.Variables)
                    scope.TryDeclareVariable(v);

                parent = scope;
            }

            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            var result = new BoundScope(null);

            foreach (var f in BuiltinFunctions.GetAll())
                result.TryDeclareFunction(f);

            return result;
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private BoundStatement BindErrorStatement()
        {
            return new BoundExpressionStatement(new BoundErrorExpression());
        }

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
        {
            return BindStatement(syntax, isGlobal: true);
        }

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            var result = BindStatementInternal(syntax);

            if (!_isScript || !isGlobal)
            {
                if (result is BoundExpressionStatement es)
                {
                    var isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                              es.Expression.Kind == BoundNodeKind.AssignmentExpression ||
                                              es.Expression.Kind == BoundNodeKind.CallExpression ||
                                              es.Expression.Kind == BoundNodeKind.ObjectAccessExpression;
                    if (!isAllowedExpression)
                        _diagnostics.ReportInvalidExpressionStatement(syntax.Location);
                }
            }

            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax)
        {

            switch (syntax.Kind)
            {
                case SyntaxKind.PackageStatement:
                    return BindPackageStatement((PackageStatementSyntax)syntax);
                case SyntaxKind.NamespaceStatement:
                    return BindNamespaceStatement((NamespaceStatementSyntax)syntax);
                case SyntaxKind.TypeStatement:
                    return BindTypeStatement((TypeStatementSyntax)syntax);
                case SyntaxKind.UseStatement:
                    return BindUseStatement((UseStatementSyntax)syntax);
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax)syntax);
                case SyntaxKind.VariableDeclaration:
                    return BindVariableDeclaration((VariableDeclarationSyntax)syntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)syntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)syntax);
                case SyntaxKind.DoWhileStatement:
                    return BindDoWhileStatement((DoWhileStatementSyntax)syntax);
                case SyntaxKind.ForStatement:
                    return BindForStatement((ForStatementSyntax)syntax);
                case SyntaxKind.FromToStatement:
                    return BindFromToStatement((FromToStatementSyntax)syntax);
                case SyntaxKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax)syntax);
                case SyntaxKind.ContinueStatement:
                    return BindContinueStatement((ContinueStatementSyntax)syntax);
                case SyntaxKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax)syntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)syntax);
                case SyntaxKind.TryCatchStatement:
                    return BindTryCatchStatement((TryCatchStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindUseStatement(UseStatementSyntax syntax)
        {
            bool found = false;
            bool alreadyFound = false;

            foreach(Package.Package p in _packageNamespaces)
            {
                if (p.name == syntax.Name.Text)
                    found = true;
            }
            foreach(string s in _usingPackages)
            {
                if (s == syntax.Name.Text)
                    alreadyFound = true;
            }

            if (!found)
            {
                _diagnostics.ReportNamespaceNotFound(syntax.Location, syntax.Name.Text);
                return BindErrorStatement();
            }

            if (alreadyFound)
            {
                _diagnostics.NamespaceCantBeUsedTwice(syntax.Location, syntax.Name.Text);
                return BindErrorStatement();
            }

            _usingPackages.Add(syntax.Name.Text);
            //Console.WriteLine("ADDED: " + syntax.Name.Text);
            return null;
        }

        private BoundStatement BindTypeStatement(TypeStatementSyntax syntax)
        {
            _type = syntax.Name.Text;
            return null;
        }

        private BoundStatement BindNamespaceStatement(NamespaceStatementSyntax syntax)
        {
            _namespace = syntax.Name.Text;
            return null;
        }

        private BoundStatement BindPackageStatement(PackageStatementSyntax syntax)
        {
            var package = syntax.Package.Text;

            if (!Package.Packager.systemPackages.ContainsKey(package) && !syntax.IsDll)
            {
                _diagnostics.ReportUnknownPackage(package);
                return BindErrorStatement();
            }

            if (syntax.IsDll)
                _packageNamespaces.Add(Package.Packager.loadPackage(package + ".dll", true));
            else
                _packageNamespaces.Add(Package.Packager.loadPackage(Package.Packager.systemPackages[package], false));
            return null;
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();
            _scope = new BoundScope(_scope, "Block");

            foreach (var statementSyntax in syntax.Statements)
            {
                var statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }

            _scope = _scope.Parent;

            return new BoundBlockStatement(statements.ToImmutable());
        }

        private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax syntax)
        {
            var isReadOnly = false;
            var type = BindTypeClause(syntax.TypeClause);
            var initializer = BindExpression(syntax.Initializer);
            var variableType = type ?? initializer.Type;
            var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType, syntax.Keyword.Kind);
            var convertedInitializer = BindConversion(syntax.Initializer.Location, initializer, syntax.ExternalType == null ? variableType : syntax.ExternalType);

            return new BoundVariableDeclaration(variable, convertedInitializer);
        }

        private TypeSymbol BindTypeClause(TypeClauseSyntax syntax)
        {
            if (syntax == null)
                return null;

            var type = LookupType(syntax.Identifier.Text);
            if (type == null)
                _diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);

            return type;
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause == null ? null : BindStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindTryCatchStatement(TryCatchStatementSyntax syntax)
        {
            var normalStatement = BindStatementInternal(syntax.NormalStatement);
            var exceptionStatement = BindStatementInternal(syntax.ExceptionSyntax);
            return new BoundTryCatchStatement(normalStatement, exceptionStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindDoWhileStatement(DoWhileStatementSyntax syntax)
        {
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            return new BoundDoWhileStatement(body, condition, breakLabel, continueLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            _scope = new BoundScope(_scope);

            var variable = BindVariableDeclaration((VariableDeclarationSyntax)syntax.Variable);
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var action = BindExpression(syntax.Action, TypeSymbol.Int);

            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);

            _scope = _scope.Parent;

            return new BoundForStatement(variable, condition, action, body, breakLabel, continueLabel);
        }

        private BoundStatement BindFromToStatement(FromToStatementSyntax syntax)
        {
            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);

            _scope = new BoundScope(_scope);

            var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly: true, TypeSymbol.Int, syntax.Keyword.Kind);
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);

            _scope = _scope.Parent;

            return new BoundFromToStatement(variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundStatement BindLoopBody(StatementSyntax body, out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            _loopStack.Push((breakLabel, continueLabel));
            var boundBody = BindStatement(body);
            _loopStack.Pop();

            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }

            var breakLabel = _loopStack.Peek().BreakLabel;
            return new BoundGotoStatement(breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }

            var continueLabel = _loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);

            if (_function == null)
            {
                if (_isScript)
                {
                    // Ignore because we allow both return with and without values.
                    if (expression == null)
                        expression = new BoundLiteralExpression("");
                }
                else if (expression != null)
                {
                    // Main does not support return values.
                    _diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression.Location);
                }
            }
            else
            {
                if (_function.Type == TypeSymbol.Void)
                {
                    if (expression != null)
                        _diagnostics.ReportInvalidReturnExpression(syntax.Expression.Location, _function.Name);
                }
                else
                {
                    if (expression == null)
                        _diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, _function.Type);
                    else
                        expression = BindConversion(syntax.Expression.Location, expression, _function.Type);
                }
            }

            return new BoundReturnStatement(expression);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression, canBeVoid: true);
            return new BoundExpressionStatement(expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol targetType)
        {
            return BindConversion(syntax, targetType);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false)
        {
            var result = BindExpressionInternal(syntax);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(syntax.Location);
                return new BoundErrorExpression();
            }

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax)
        {
            if (syntax == null)
                return new BoundErrorExpression();

            switch (syntax.Kind)
            {
                case SyntaxKind.ParenthesizedExpression:
                    return BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax);
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);
                case SyntaxKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax)syntax);
                case SyntaxKind.ObjectAccessExpression:
                    return BindObjectAccessExpression((ObjectAccessExpression)syntax);
                case SyntaxKind.AssignmentExpression:
                    return BindAssignmentExpression((AssignmentExpressionSyntax)syntax);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)syntax);
                case SyntaxKind.ThreadCreateExpression:
                    return BindThreadCreateExpression((ThreadCreationSyntax)syntax);
                case SyntaxKind.ArrayCreateExpression:
                    return BindArrayCreationExpression((ArrayCreationSyntax)syntax);
                case SyntaxKind.ObjectCreateExpression:
                    return BindObjectCreationExpression((ObjectCreationSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundExpression BindObjectCreationExpression(ObjectCreationSyntax syntax)
        {
            ClassSymbol _class = (syntax.Package == null ? ParentScope : _packageNamespaces.FirstOrDefault(x => x.name == syntax.Package.Text).scope).GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.Type.Text);
            Package.Package package = null;

            if (syntax.Package != null)
                package = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Package.Text);

            if (_class == null)
            {
                foreach (string s in _usingPackages)
                {
                    var _package = _packageNamespaces.FirstOrDefault(x => x.name == s);

                    if (_package != null)
                    {
                        _class = _package.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.Type.Text);
                        if (_class != null)
                        {
                            package = _package;
                            break;
                        }
                    }
                }
            }

            if (_class == null)
            {
                _diagnostics.ReportClassNotFound(syntax.Type.Location, syntax.Type.Text);
                return new BoundErrorExpression();
            }

            if (_class.IsStatic)
            {
                _diagnostics.ReportCantMakeInstanceOfStaticClass(syntax.Type.Location, syntax.Type.Text);
                return new BoundErrorExpression();
            }

            FunctionSymbol constructorFunction = _class.Scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == "Constructor");

            if (constructorFunction == null && syntax.Arguments.Count != 0)
            {
                _diagnostics.ReportWrongNumberOfConstructorArgs(syntax.Location, syntax.Type.Text, 0, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            if (constructorFunction != null)
            if (constructorFunction.Parameters.Length != syntax.Arguments.Count)
            {
                _diagnostics.ReportWrongNumberOfConstructorArgs(syntax.Location, syntax.Type.Text, constructorFunction.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                var boundArgument = BindConversion(syntax.Location, BindExpression(syntax.Arguments[i]), constructorFunction.Parameters[i].Type);
                boundArguments.Add(boundArgument);
            }

            return new BoundObjectCreationExpression(_class, boundArguments.ToImmutable(), package);
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser. We already
                // reported error so we can just return an error expression.
                return new BoundErrorExpression();
            }

            var variable = BindVariableReference(syntax.IdentifierToken);
            if (variable == null)
                return new BoundErrorExpression();

            if (syntax.isArray)
            {
                var index = BindExpressionInternal(syntax.Index);
                return new BoundVariableExpression(variable, index, ArrayToType(variable.Type));
            }

            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindObjectAccessExpression(ObjectAccessExpression syntax)
        {
            FunctionSymbol function = null;
            BoundCallExpression typeCall = null;
            ImmutableArray<BoundExpression> arguments = new ImmutableArray<BoundExpression>();
            VariableSymbol property = null;
            TypeSymbol type = TypeSymbol.Any;
            BoundExpression value = null;
            Package.Package package = null;
            BoundExpression Expression = null;
            ClassSymbol staticClass = null;

            if (syntax.Expression != null)
            {
                Expression = BindExpression(syntax.Expression);
                goto SKIP1;
            }

            if (syntax.IdentifierToken.Text == "Main")
            {
                staticClass = new ClassSymbol("Main", null , true);
                staticClass.Scope = new BoundScope(null);
                goto SKIP1;
            }

            if (syntax.IdentifierToken.Text == null)
            {
                _diagnostics.ReportUndefinedVariable(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression();
            }

            staticClass = ParentScope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);

            if (syntax.Package != null)
            {
                package = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Package.Text);

                if (package == null)
                    goto SKIP0;

                staticClass = package.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);
            }

            SKIP0:

            if (syntax.Package == null && staticClass == null)
            {
                foreach (string s in _usingPackages)
                {
                    package = _packageNamespaces.FirstOrDefault(x => x.scope.TryLookupSymbol(syntax.IdentifierToken.Text) != null);
                    if (package == null) continue;

                    //var sym = package.scope.TryLookupSymbol(syntax.IdentifierToken.Text);

                }

                if (package == null) goto SKIP1;

                staticClass = package.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);
            }

            SKIP1:

            VariableSymbol variable = null;
            ClassSymbol _class = null;

            if (staticClass == null)
            {
                if (syntax.Expression == null)
                {
                    variable = BindVariableReference(syntax.IdentifierToken);

                    if (variable == null)
                    {
                        _diagnostics.ReportUndefinedVariable(syntax.IdentifierToken.Location,
                            syntax.IdentifierToken.Text);
                        return new BoundErrorExpression();
                    }

                    if (!variable.Type.isClass || variable.Type.isClassArray)
                    {
                        typeCall = (BoundCallExpression) BindCallExpression(syntax.Call, true);
                        type = typeCall.Type;
                        return new BoundObjectAccessExpression(variable, syntax.Type, function, arguments, property,
                            type, value, package, _class, typeCall, Expression);
                    }
                    
                    _class = ParentScope.GetDeclaredClasses().FirstOrDefault(x => x.Name == variable.Type.Name);
                }
                else
                {
                    variable = new LocalVariableSymbol("", true, Expression.Type);
                    
                    if (!Expression.Type.isClass)
                    {
                        typeCall = (BoundCallExpression) BindCallExpression(syntax.Call);
                        type = typeCall.Type;
                        return new BoundObjectAccessExpression(variable, syntax.Type, function, arguments, property,
                            type, value, package, _class, typeCall, Expression);
                    }

                    _class = ParentScope.GetDeclaredClasses().FirstOrDefault(x => x.Name == Expression.Type.Name);
                }
            }
            else
                _class = staticClass;

            if (_class == null)
            {
                _class = _packageNamespaces.SelectMany(x => x.scope.GetDeclaredClasses()).FirstOrDefault(x => x.Name == variable.Type.Name);

                if (_class != null)
                    package = _packageNamespaces.FirstOrDefault(x => x.scope.TryLookupSymbol(_class.Name) != null);
            }

            if (_class == null)
            {
                return new BoundErrorExpression();
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Call)
            {
                Symbol symbol = _class.Scope.TryLookupSymbol(syntax.Call.Identifier.Text);

                if (_class.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.Call.Identifier.Text);

                if (symbol == null || !(symbol is FunctionSymbol))
                {
                    _diagnostics.ReportFunctionNotFoundInObject(syntax.Call.Location, syntax.Call.Identifier.Text, _class.Name);
                    return new BoundErrorExpression();
                }

                function = symbol as FunctionSymbol;
                type = function.Type;

                var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

                foreach (var argument in syntax.Call.Arguments)
                {
                    var boundArgument = BindExpression(argument);
                    boundArguments.Add(boundArgument);
                }

                arguments = boundArguments.ToImmutable();

                if (function.Parameters.Length != boundArguments.Count)
                {
                    _diagnostics.ReportFunctionInObjectHasDifferentParams(syntax.Call.Location, syntax.Call.Identifier.Text, _class.Name, boundArguments.Count);
                    return new BoundErrorExpression();
                }
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Get)
            {
                var symbol = _class.Scope.TryLookupSymbol(syntax.LookingFor.Text);
                if (_class.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.LookingFor.Text);

                if (symbol == null || !(symbol is VariableSymbol))
                {
                    _diagnostics.ReportVariableNotFoundInObject(syntax.LookingFor.Location, syntax.LookingFor.Text, _class.Name);
                    return new BoundErrorExpression();
                }

                property = symbol as VariableSymbol;
                type = property.Type;
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Set)
            {
                var symbol = _class.Scope.TryLookupSymbol(syntax.LookingFor.Text);
                if (_class.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.LookingFor.Text);

                if (symbol == null || !(symbol is VariableSymbol))
                {
                    _diagnostics.ReportVariableNotFoundInObject(syntax.LookingFor.Location, syntax.LookingFor.Text, _class.Name);
                    return new BoundErrorExpression();
                }

                property = symbol as VariableSymbol;
                type = property.Type;
                value = BindExpression(syntax.Value);
                value = BindConversion(syntax.Value.Location, value, type);
                type = TypeSymbol.Void;
            }

            return new BoundObjectAccessExpression(variable, syntax.Type, function, arguments, property, type, value, package, _class, typeCall, Expression);
        }

        private BoundExpression BindArrayCreationExpression(ArrayCreationSyntax syntax)
        {
            var type = LookupType(syntax.Type.Text);
            var length = BindExpressionInternal(syntax.Length);
            var arrType = TypeToArray(type);

            return new BoundArrayCreationExpression(type, length, arrType);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            var variable = BindVariableReference(syntax.IdentifierToken);
            if (variable == null)
                return boundExpression;

            if (variable.IsReadOnly)
                _diagnostics.ReportCannotAssign(syntax.EqualsToken.Location, name);

            if (syntax.isArray)
            {
                var cExpression = BindConversion(syntax.Expression.Location, boundExpression, ArrayToType(variable.Type));
                var boundIndex = BindExpression(syntax.Index);
                return new BoundAssignmentExpression(variable, cExpression, boundIndex);
            }

            var convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type);

            return new BoundAssignmentExpression(variable, convertedExpression);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Type == TypeSymbol.Error)
                return new BoundErrorExpression();

            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundOperand.Type);
                return new BoundErrorExpression();
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
                return new BoundErrorExpression();

            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (boundOperator == null)
            {
                if (Conversion.Classify(boundLeft.Type, boundRight.Type) == Conversion.Implicit)
                {
                    var newLeft = BindConversion(syntax.Left.Location, boundLeft, boundRight.Type);
                    boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, newLeft.Type, boundRight.Type);

                    if (boundOperator != null)
                        boundLeft = newLeft;
                }
                
                if (boundOperator == null && Conversion.Classify(boundRight.Type, boundLeft.Type) == Conversion.Implicit)
                {
                    var newRight = BindConversion(syntax.Right.Location, boundRight, boundLeft.Type);
                    boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, newRight.Type);

                    if (boundOperator != null)
                        boundRight = newRight;
                }
            }

            if (boundOperator == null)
            {
                _diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression();
            }

            return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
        }

        private BoundExpression BindThreadCreateExpression(ThreadCreationSyntax syntax)
        {
            var symbol = _scope.TryLookupSymbol(syntax.Identifier.Text);

            if (symbol == null)
            {
                _diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            var function = symbol as FunctionSymbol;
            if (function == null)
            {
                _diagnostics.ReportNotAFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            return new BoundThreadCreateExpression(function);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, bool objectAccess = false)
        {
            if (syntax == null)
            {
                _diagnostics.ReportCustomeMessage("Call syntax was null (maybe wrong params?)");
                return new BoundErrorExpression();
            }

            var _symbol = _scope.TryLookupSymbol(syntax.Identifier.Text);

            if (syntax.Identifier.Text == "Write" && !objectAccess)
                _symbol = null;
            
            if (_symbol == null)
            {
                foreach (string s in _usingPackages)
                {
                    var pack = _packageNamespaces.FirstOrDefault(x => x.name == s);
                    if (pack != null && pack.scope.TryLookupSymbol(syntax.Identifier.Text) != null)
                    {
                        syntax.Namespace = s;
                        break;
                    }
                }
            }

            if (syntax.Namespace != "")
            {
                bool found = false;

                foreach (Package.Package p in _packageNamespaces)
                {
                    if (p.name == syntax.Namespace)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Console.WriteLine("namespace not found!");
                    _diagnostics.ReportNamespaceNotFound(syntax.Location, syntax.Namespace);
                    return new BoundErrorExpression();
                }
            }


            if (syntax.Arguments.Count == 1 && LookupType(syntax.Identifier.Text) is TypeSymbol type)
                return BindConversion(syntax.Arguments[0], type, allowExplicit: true);

            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArguments.Add(boundArgument);
            }

            var symbol = (syntax.Namespace == "" ? _scope : _packageNamespaces.FirstOrDefault(x => x.name == syntax.Namespace).scope).TryLookupSymbol(syntax.Identifier.Text);
            if (symbol == null)
            {
                _diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            var function = symbol as FunctionSymbol;
            if (function == null)
            {
                _diagnostics.ReportNotAFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (syntax.Arguments.Count != function.Parameters.Length)
            {
                TextSpan span;
                if (syntax.Arguments.Count > function.Parameters.Length)
                {
                    SyntaxNode firstExceedingNode;
                    if (function.Parameters.Length > 0)
                        firstExceedingNode = syntax.Arguments.GetSeparator(function.Parameters.Length - 1);
                    else
                        firstExceedingNode = syntax.Arguments[0];
                    var lastExceedingArgument = syntax.Arguments[syntax.Arguments.Count - 1];
                    span = TextSpan.FromBounds(firstExceedingNode.Span.Start, lastExceedingArgument.Span.End);
                }
                else
                {
                    span = syntax.CloseParenthesisToken.Span;
                }
                var location = new TextLocation(syntax.SyntaxTree.Text, span);
                _diagnostics.ReportWrongArgumentCount(location, function.Name, function.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            try
            {
                for (var i = 0; i < syntax.Arguments.Count; i++)
                {
                    if (syntax.Arguments[i] == null)
                    {
                        _diagnostics.ReportCustomeMessage("Undefined Parameter!");
                        return new BoundErrorExpression();
                    }

                    var argumentLocation = syntax.Arguments[i].Location;
                    var argument = boundArguments[i];
                    var parameter = function.Parameters[i];
                    boundArguments[i] = BindConversion(argumentLocation, argument, parameter.Type);
                }
            }
            catch {_diagnostics.ReportCustomeMessage($"Parameter binding crashed on function {syntax.Identifier.Text}!"); return new BoundErrorExpression(); }

            return new BoundCallExpression(function, boundArguments.ToImmutable(), syntax.Namespace);
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax);
             return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression, TypeSymbol type, bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                    _diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);

                return new BoundErrorExpression();
            }

            if (!allowExplicit && conversion.IsExplicit)
            {
                _diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);
            }

            if (conversion.IsIdentity)
                return expression;

            return new BoundConversionExpression(type, expression);
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol type, SyntaxKind syntax)
        {
            var name = identifier.Text ?? "?";
            var declare = !identifier.IsMissing;
            var variable = syntax == SyntaxKind.SetKeyword
                                ? (VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, type)
                                : new LocalVariableSymbol(name, isReadOnly, type);

            //Console.WriteLine($"VAR: {name} | {declare} | {variable} | {(variable.IsGlobal ? getClassScope() : _scope).Name}");

            if (declare && variable.IsGlobal ? !getClassScope().TryDeclareVariable(variable) : !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);

            return variable;
        }

        private VariableSymbol BindVariableReference(SyntaxToken identifierToken)
        {
            var name = identifierToken.Text;
            object var = _scope.TryLookupSymbol(name);
            if (var == null)
                var = getClassScope().TryLookupSymbol(name);

            switch (var)
            {
                case VariableSymbol variable:
                    return variable;

                case null:
                    _diagnostics.ReportUndefinedVariable(identifierToken.Location, name);
                    return null;

                default:
                    _diagnostics.ReportNotAVariable(identifierToken.Location, name);
                    return null;
            }
        }

        public static TypeSymbol LookupType(string name)
        {
            switch (name)
            {
                case "void":
                    return TypeSymbol.Void;

                case "any":
                    return TypeSymbol.Any;
                case "bool":
                    return TypeSymbol.Bool;
                case "int":
                    return TypeSymbol.Int;
                case "string":
                    return TypeSymbol.String;
                case "float":
                    return TypeSymbol.Float;
                case "thread":
                    return TypeSymbol.Thread;
                case "tcpclient":
                    return TypeSymbol.TCPClient;
                case "tcplistener":
                    return TypeSymbol.TCPListener;
                case "tcpsocket":
                    return TypeSymbol.TCPSocket;

                case "anyArr":
                    return TypeSymbol.AnyArr;
                case "boolArr":
                    return TypeSymbol.BoolArr;
                case "intArr":
                    return TypeSymbol.IntArr;
                case "stringArr":
                    return TypeSymbol.StringArr;
                case "floatArr":
                    return TypeSymbol.FloatArr;
                case "threadArr":
                    return TypeSymbol.ThreadArr;
                case "tcpclientArr":
                    return TypeSymbol.TCPClientArr;
                case "tcplistenerArr":
                    return TypeSymbol.TCPListenerArr;
                case "tcpsocketArr":
                    return TypeSymbol.TCPSocketArr;
                default:
                    if (TypeSymbol.Class == null) TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>();
                    return TypeSymbol.Class.Values.FirstOrDefault(x => x.Name == name);;
            }
        }
        private TypeSymbol TypeToArray(TypeSymbol type)
        {
            if (type == null)
                return null;

            if (type == TypeSymbol.Any)
                return TypeSymbol.AnyArr;
            else if (type == TypeSymbol.Bool)
                return TypeSymbol.BoolArr;
            else if (type == TypeSymbol.Int)
                return TypeSymbol.IntArr;
            else if (type == TypeSymbol.String)
                return TypeSymbol.StringArr;
            else if (type == TypeSymbol.Float)
                return TypeSymbol.FloatArr;
            else if (type == TypeSymbol.Thread)
                return TypeSymbol.ThreadArr;
            else if (type == TypeSymbol.TCPClient)
                return TypeSymbol.TCPClientArr;
            else if (type == TypeSymbol.TCPListener)
                return TypeSymbol.TCPListenerArr;
            else if (type == TypeSymbol.TCPSocket)
                return TypeSymbol.TCPSocketArr;
            else if (type.isClass)
                return TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == type.Name + "Arr").Value;

            return null;
        }
        private TypeSymbol ArrayToType(TypeSymbol type)
        {
            if (type == TypeSymbol.AnyArr)
                return TypeSymbol.Any;
            else if (type == TypeSymbol.BoolArr)
                return TypeSymbol.Bool;
            else if (type == TypeSymbol.IntArr)
                return TypeSymbol.Int;
            else if (type == TypeSymbol.StringArr)
                return TypeSymbol.String;
            else if (type == TypeSymbol.FloatArr)
                return TypeSymbol.Float;
            else if (type == TypeSymbol.ThreadArr)
                return TypeSymbol.Thread;
            else if (type == TypeSymbol.TCPClientArr)
                return TypeSymbol.TCPClient;
            else if (type == TypeSymbol.TCPListenerArr)
                return TypeSymbol.TCPListener;
            else if (type == TypeSymbol.TCPSocketArr)
                return TypeSymbol.TCPSocket;
            else if (type.isClass)
                return TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == type.Name.Substring(0, type.Name.Length - 3)).Value;

            return type;
        }
    }
}
