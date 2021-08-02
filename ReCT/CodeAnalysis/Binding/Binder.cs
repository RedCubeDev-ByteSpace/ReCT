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
        private FunctionSymbol _function;

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        private int _labelCounter;
        private BoundScope _scope;
        private ClassSymbol inClass = null;
        static BoundScope ParentScope;

        public static List<Package.Package> _packageNamespaces = new List<Package.Package>();
        public static string _namespace = "";
        public static string _type = "";
        public static List<string> _usingPackages = new List<string>();
        public static Dictionary<string,string> _packageAliases = new Dictionary<string,string>();

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
            
            var enumDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<EnumDeclarationSyntax>();

            var globalStatements = syntaxTrees.SelectMany(st => st.Root.Members)
                .OfType<GlobalStatementSyntax>();
            
            //package imports BEFORE anything else
            foreach (var statement in globalStatements)
            {
                if (statement.Statement is PackageStatementSyntax p)
                {
                    binder.BindGlobalStatement(p);
                    if (Compilation.PrintDebugMessages) Console.WriteLine("imported: " + p.Package.Text);
                }
                if (statement.Statement is UseStatementSyntax u)
                {
                    binder.BindGlobalStatement(u);
                    if (Compilation.PrintDebugMessages) Console.WriteLine("using: " + u.Name.Text);
                }
                if (statement.Statement is AliasStatementSyntax a)
                {
                    binder.BindGlobalStatement(a);
                    if (Compilation.PrintDebugMessages) Console.WriteLine("aliasing: " + a.MapThis.Text);
                }
            }

            //bind enums classes functions and class-functions
            foreach (var _enum in enumDeclarations)
                binder.BindEnumDeclaration(_enum);

            //abstract classes first
            foreach (var _class in classDeclarations)
                if (_class.IsAbstract)
                    binder.BindClassDeclaration(_class);

            //others later bc they might inherit
            foreach (var _class in classDeclarations)
                if (!_class.IsAbstract)
                binder.BindClassDeclaration(_class);

            foreach (var function in functionDeclarations)
                binder.BindFunctionDeclaration(function);

            foreach (var _class in binder._scope.GetDeclaredClasses())
                binder.BindClassMemberDeclaration(_class.Declaration, _class);
            

            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (var globalStatement in globalStatements)
            {
                if (globalStatement.Statement is PackageStatementSyntax || globalStatement.Statement is UseStatementSyntax|| globalStatement.Statement is AliasStatementSyntax) continue;
                
                var statement = binder.BindGlobalStatement(globalStatement.Statement);
                statements.Add(statement);
            }

            var retstmt = binder.BindReturnStatement(new ReturnStatementSyntax(null, null, null));
            statements.Add(retstmt);

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
            var enums = binder._scope.GetDeclaredEnums();

            if (previous != null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, mainFunction, scriptFunction, functions, variables, statements.ToImmutable(), classes, enums);
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram previous, BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            var classBodies = ImmutableDictionary.CreateBuilder<ClassSymbol, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>>();
            var enums = ImmutableArray.CreateBuilder<EnumSymbol>();
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
                    var body = (BoundBlockStatement)binder.BindStatement(fs.Declaration.Body);
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

            foreach (var _enum in globalScope.Enums)
            {
                enums.Add(_enum);
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
                                    enums.ToImmutableArray(),
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
            if (inClass == null && syntax.IsVirtual)
                _diagnostics.ReportVirtualFunctionInMain(syntax.Location);

            if (inClass != null && !inClass.IsAbstract && syntax.IsVirtual)
                _diagnostics.ReportCantUseVirtFuncInNormalClass(syntax.Location);

            if (syntax.Identifier.Text == "main")
                _diagnostics.ReportFunctionCantBeCalledMain(syntax.Location);

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

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax, syntax.IsPublic, isVirtual: syntax.IsVirtual, isOverride: syntax.IsOverride);
            if (function.Declaration.Identifier.Text != null &&
                !_scope.TryDeclareFunction(function))
            {
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
            }
        }

        private void BindClassDeclaration(ClassDeclarationSyntax syntax)
        {
            if (syntax.IsStatic && syntax.IsAbstract)
                _diagnostics.ReportClassCantBeAbstractAndStatic(syntax.Location, syntax.Identifier.Text);

            ClassSymbol parentSym = null;

            if (syntax.Inheritance != null)
            {
                var symbol = _scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.Inheritance.Text);
                if (symbol == null)
                    _diagnostics.ReportCouldtFindClassToInheritFrom(syntax.Inheritance.Location, syntax.Identifier.Text, syntax.Inheritance.Text);
                else if (!(symbol is ClassSymbol))
                    _diagnostics.ReportCouldtFindClassToInheritFrom(syntax.Inheritance.Location, syntax.Identifier.Text, syntax.Inheritance.Text);
                else
                    parentSym = (ClassSymbol)symbol;

                if (parentSym != null && !parentSym.IsAbstract)
                    _diagnostics.ReportInheratingClassNeedsToBeAbstract(syntax.Inheritance.Location, syntax.Inheritance.Text);
                if (parentSym != null && syntax.IsAbstract)
                    _diagnostics.ReportCantInheritWithAbstractClass(syntax.Inheritance.Location, syntax.Identifier.Text);
            }

            var classSymbol = new ClassSymbol(syntax.Identifier.Text, syntax, syntax.IsStatic, syntax.IsIncluded, syntax.IsAbstract, syntax.IsSerializable, parentSym);
            classSymbol.Scope = new BoundScope(CreateRootScope(), syntax.Identifier.Text);

            if (classSymbol.Name.Contains("Arr"))
            {
                _diagnostics.ReportSymbolHasKeywordArr(syntax.Identifier.Location, "Class", classSymbol.Name);
            }
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

        private void BindEnumDeclaration(EnumDeclarationSyntax syntax)
        {
            int counter = 0;
            Dictionary<string, int> values = new Dictionary<string, int>();

            for (int i = 0; i < syntax.Names.Length; i++)
            {
                if (values.ContainsKey(syntax.Names[i].Text))
                    _diagnostics.ReportInvalidEnumNames(syntax.Names[i].Location, syntax.EnumName.Text, syntax.Names[i].Text);

                if (syntax.Values.ContainsKey(syntax.Names[i]))
                {
                    var literal = (BoundLiteralExpression)BindLiteralExpression((LiteralExpressionSyntax)syntax.Values[syntax.Names[i]]);
                    if (literal.Type != TypeSymbol.Int)
                    {
                        _diagnostics.ReportInvalidEnumType(syntax.Names[i].Location, syntax.EnumName.Text);
                    }
                    values.Add(syntax.Names[i].Text, (int)literal.Value);
                    counter = (int)literal.Value + 1;
                    continue;
                }

                values.Add(syntax.Names[i].Text, counter);
                counter++;
            }

            if (TypeSymbol.Class == null) {TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>(); }

            var enumTypeSymbol = new TypeSymbol(syntax.EnumName.Text);
            enumTypeSymbol.isClass = true;
            enumTypeSymbol.isEnum = true;
            
            var enumSymbol = new EnumSymbol(syntax.EnumName.Text, values, enumTypeSymbol);
            enumTypeSymbol.enumSymbol = enumSymbol;

            if (enumSymbol.Name.Contains("Arr"))
            {
                _diagnostics.ReportSymbolHasKeywordArr(syntax.EnumName.Location, "Enum", enumSymbol.Name);
            }

            TypeSymbol.Class.Add(new ClassSymbol(syntax.EnumName.Text, null, true), enumTypeSymbol);

            if (!_scope.TryDeclareEnum(enumSymbol))
            {
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.EnumName.Location, enumSymbol.Name);
            }
        }

        private void BindClassMemberDeclaration(ClassDeclarationSyntax syntax, ClassSymbol _class)
        {
            inClass = _class;
            var prevScope = _scope;
            _scope = _class.Scope;
            
            foreach (MemberSyntax m in syntax.Members)
            {
                if (m is GlobalStatementSyntax glsyntax)
                {
                    if (glsyntax.Statement is VariableDeclarationSyntax vsyntax)
                    {
                        BindVariableDeclaration(vsyntax);
                    }
                }

                if (m is FunctionDeclarationSyntax fsyntax)
                {
                    if (!_class.IsAbstract && fsyntax.IsVirtual)
                        _diagnostics.ReportCantUseVirtFuncInNormalClass(syntax.Location);

                    if (fsyntax.IsVirtual && !fsyntax.IsPublic)
                        _diagnostics.ReportVirtualFunctionsNeedToBePublic(syntax.Location);

                    if (_class.ParentSym == null && fsyntax.IsOverride)
                        _diagnostics.ReportCantUseOvrFuncInNormalClass(syntax.Location);

                    if (fsyntax.IsOverride && !fsyntax.IsPublic)
                        _diagnostics.ReportOverridingFunctionsNeedToBePublic(syntax.Location);

                    FunctionSymbol overriding = null;
                    if (fsyntax.IsOverride)
                    {
                        //look for function its supposed to replace
                        var replacing = _class.ParentSym.Scope.TryLookupSymbol(fsyntax.Identifier.Text);
                        if (replacing == null)
                            _diagnostics.ReportFunctionToOverrideNotFound(syntax.Location, fsyntax.Identifier.Text);
                        if (!(replacing is FunctionSymbol))
                            _diagnostics.ReportFunctionToOverrideNotFound(syntax.Location, fsyntax.Identifier.Text);

                        if (fsyntax.Parameters.Count != (replacing as FunctionSymbol).Parameters.Length)
                            _diagnostics.ReportOverridingFunctionsParametersNeedToBeTheSame(syntax.Location, fsyntax.Identifier.Text);
                        
                        overriding = replacing as FunctionSymbol;
                    }

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

                    if (fsyntax.IsOverride)
                        if (type != overriding.Type)
                            _diagnostics.ReportOverridingFunctionsTypeNeedsToBeTheSame(syntax.Location, fsyntax.Identifier.Text);

                    if (fsyntax.Identifier.Text == "Constructor")
                    {
                        var index = 0;
                        var bodyb = fsyntax.Body.Statements.ToBuilder();

                        foreach (MemberSyntax mem in syntax.Members)
                            if (mem is GlobalStatementSyntax gsyntax)
                            {
                                if (gsyntax.Statement is VariableDeclarationSyntax)
                                {
                                    bodyb.Insert(index, gsyntax.Statement);
                                    index++;
                                }
                            }

                        fsyntax.Body.Statements = bodyb.ToImmutable();

                        if (_class.ParentSym != null)
                        {
                            var baseStatement = fsyntax.Body.Statements.FirstOrDefault(x => x.Kind == SyntaxKind.BaseStatement);
                            if (baseStatement == null)
                            {
                                _diagnostics.ReportBaseConstructorCallRequired(fsyntax.Location, fsyntax.Identifier.Text);
                            }
                        }
                    }

                    var function = new FunctionSymbol(fsyntax.Identifier.Text, parameters.ToImmutable(), type, fsyntax, fsyntax.IsPublic, isVirtual: fsyntax.IsVirtual, isOverride: fsyntax.IsOverride);
                    if (function.Declaration.Identifier.Text != null &&
                        !_class.Scope.TryDeclareFunction(function))
                    {
                        _diagnostics.ReportSymbolAlreadyDeclared(fsyntax.Identifier.Location, function.Name);
                    }
                }
            }

            _class.Scope = _scope;
            _scope = prevScope;
            inClass = null;

            //register constructor if not registered
            var constructor = _class.Scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == "Constructor");
            if (constructor == null)
            {
                var builder = ImmutableArray.CreateBuilder<StatementSyntax>();

                if (_class.ParentSym != null)
                {
                    var func = _class.ParentSym.Scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == "Constructor");

                    if (func == null)
                        builder.Add(new BaseStatementSyntax(null, null, new SeparatedSyntaxList<ExpressionSyntax>(ImmutableArray<SyntaxNode>.Empty)));
                    else if (func.Parameters.Length == 0)
                        builder.Add(new BaseStatementSyntax(null, null, new SeparatedSyntaxList<ExpressionSyntax>(ImmutableArray<SyntaxNode>.Empty)));
                    else
                        _diagnostics.ReportBaseConstructorCallRequired(_class.Declaration.Location, _class.Name);
                }

                foreach (MemberSyntax mem in syntax.Members)
                if (mem is GlobalStatementSyntax gsyntax)
                    if (gsyntax.Statement is VariableDeclarationSyntax)
                        builder.Add(gsyntax.Statement);

                var dec = new FunctionDeclarationSyntax(null, null, null, null, null, null, null, new BlockStatementSyntax(null, null, builder.ToImmutable(), null), true, false, false);

                var function = new FunctionSymbol("Constructor", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, dec, true);
                _class.Scope.TryDeclareFunction(function);
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
                case SyntaxKind.AliasStatement:
                    return BindAliasStatement((AliasStatementSyntax)syntax);
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
                case SyntaxKind.BaseStatement:
                    return BindBaseStatement((BaseStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindUseStatement(UseStatementSyntax syntax)
        {
            var found = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Name.Text);
            var alreadyFound = _usingPackages.FirstOrDefault(x => x == syntax.Name.Text);

            if (found == null && _packageAliases.ContainsKey(syntax.Name.Text))
                found = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[syntax.Name.Text]);

            if (found == null)
            {
                _diagnostics.ReportNamespaceNotFound(syntax.Location, syntax.Name.Text);
                return BindErrorStatement();
            }

            if (alreadyFound != null)
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

        private BoundStatement BindAliasStatement(AliasStatementSyntax syntax)
        {
            var mapthis = _packageNamespaces.FirstOrDefault(x => x.name == syntax.MapThis.Text);
            if (mapthis == null)
            {
                _diagnostics.ReportAliasSourceMissing(syntax.Location, syntax.MapThis.Text);
                return null;
            }

            var tothis = _packageNamespaces.FirstOrDefault(x => x.name == syntax.ToThis.Text);
            if (tothis != null)
            {
                _diagnostics.ReportAliasTargetAlreadyRegistered(syntax.Location, syntax.MapThis.Text, syntax.ToThis.Text);
                return null;
            }

            _packageAliases.Add(syntax.ToThis.Text, syntax.MapThis.Text);
            
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

            if (syntax.Initializer == null)
            {
                return new BoundVariableDeclaration(BindVariableDeclaration(syntax.Identifier, isReadOnly, type ?? TypeSymbol.Any, syntax.Keyword.Kind, syntax.IsVirtual, syntax.IsOverride), null);
            }

            var initializer = BindExpression(syntax.Initializer);
            var variableType = type ?? initializer.Type;
            var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType, syntax.Keyword.Kind, syntax.IsVirtual, syntax.IsOverride);
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

        private BoundStatement BindBaseStatement(BaseStatementSyntax syntax)
        {
            if (inClass == null)
            {
                _diagnostics.ReportBaseNotAllowedInMain(syntax.Location);
                return BindErrorStatement();
            }

            if (inClass.ParentSym == null)
            {
                _diagnostics.ReportBaseNotAllowedInNonInheratingClass(syntax.Location);
                return BindErrorStatement();
            }

            if (_function.Name != "Constructor")
            {
                _diagnostics.ReportBaseNotAllowedInNonConstructorMethods(syntax.Location);
                return BindErrorStatement();
            }

            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArguments.Add(boundArgument);
            }

            var func = inClass.ParentSym.Scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == "Constructor");
            if (func == null)
            {
                if (boundArguments.Count != 0)
                    _diagnostics.ReportWrongArgumentCount(syntax.Location, inClass.ParentSym.Name + "->Constructor", 0, boundArguments.Count);      
            }
            else if (boundArguments.Count != func.Parameters.Length)
                _diagnostics.ReportWrongArgumentCount(syntax.Location, inClass.ParentSym.Name + "->Constructor", func.Parameters.Length, boundArguments.Count); 

            if (func != null)
            for (int i = 0; i < boundArguments.Count; i++)
            {
                if (boundArguments[i].Type != func.Parameters[i].Type)
                {
                    _diagnostics.ReportWrongArgumentType(syntax.Arguments[i].Location, inClass.ParentSym.Name + "->Constructor", i, func.Parameters[i].Name, func.Parameters[i].Type.Name, boundArguments[i].Type.Name);
                }
            }

            return new BoundBaseStatement(boundArguments.ToImmutable());
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

            var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly: true, TypeSymbol.Int, syntax.Keyword.Kind, false, false);
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
            {
                _diagnostics.ReportCustomeMessage("The given Syntax is null!");
                return new BoundErrorExpression();
            }

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
                case SyntaxKind.ActionCreateExpression:
                    return BindActionCreateExpression((ActionCreationSyntax)syntax);
                case SyntaxKind.ArrayCreateExpression:
                    return BindArrayCreationExpression((ArrayCreationSyntax)syntax);
                case SyntaxKind.ArrayLiteralExpression:
                    return BindArrayLiteralExpression((ArrayLiteralExpressionSyntax)syntax);
                case SyntaxKind.ObjectCreateExpression:
                    return BindObjectCreationExpression((ObjectCreationSyntax)syntax);
                case SyntaxKind.TernaryExpression:
                    return BindTernaryExpression((TernaryExpressionSyntax)syntax);
                case SyntaxKind.LambdaExpression:
                    return BindLambdaExpression((LambdaExpressionSyntax)syntax);
                case SyntaxKind.IsExpression:
                    return BindIsExpression((IsExpressionSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundExpression BindObjectCreationExpression(ObjectCreationSyntax syntax)
        {
            Package.Package package = null;

            if (syntax.Package != null)
                package = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Package.Text);

            if (package != null && _packageAliases.ContainsKey(syntax.Package.Text))
                package = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[syntax.Package.Text]);

            ClassSymbol _class = (package == null ? ParentScope : package.scope).GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.Type.Text);
            
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

            if (_class.IsAbstract)
            {
                _diagnostics.ReportCantMakeInstanceOfAbstractClass(syntax.Type.Location, syntax.Type.Text);
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
                // This means the token was inserted by the parser. It already reported so it can just return an error expression.
                return new BoundErrorExpression();
            }

            ClassSymbol _class = null;
            var variable = BindVariableReference(syntax.IdentifierToken, true);
            if (variable == null && inClass != null && inClass.ParentSym != null)
            {
                var baseVar = inClass.ParentSym.Scope.TryLookupSymbol(syntax.IdentifierToken.Text);
                if (baseVar != null && baseVar is VariableSymbol v)
                    variable = v;

                _class = inClass.ParentSym;
            }

            if (variable == null)
            {
                _diagnostics.ReportUndefinedVariable(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression();
            }

            if (variable.IsFunctional && inClass != null && inClass.ParentSym != null)
                _class = inClass.ParentSym;

            if (syntax.isArray)
            {
                var index = BindExpressionInternal(syntax.Index);
                return new BoundVariableExpression(variable, index, ArrayToType(variable.Type), _class);
            }

            return new BoundVariableExpression(variable, _class);
        }

        private BoundExpression BindObjectAccessExpression(ObjectAccessExpression syntax)
        {
            if (syntax.Expression != null)
            {
                return BindExpressionAccessExpression(syntax);
            }

            if (syntax.IdentifierToken.Text == null)
            {
                //if its not an expression and still null we dont have any value to access
                _diagnostics.ReportUndefinedVariable(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression();
            }
            else
            {
                var staticClass = ParentScope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);

				if (syntax.IdentifierToken.Text == "Main")
				{
					if (_function.Name == "Constructor")
					{
						_diagnostics.ReportUnableToAccessMainInConstructor(syntax.IdentifierToken.Location);
                		return new BoundErrorExpression();
					}

					staticClass = new ClassSymbol("Main", null , true);
                	staticClass.Scope = new BoundScope(null);
				}

                if (staticClass != null) return BindAccessExpressionSuffix(syntax, new BoundObjectAccessExpression(null, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, null, staticClass, null, null, null), null, staticClass, null);
                Package.Package package = null;

                // if accessing from a package look for the package
                if (syntax.Package != null)
                {
                    package = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Package.Text);

                    if(package == null && _packageAliases.ContainsKey(syntax.Package.Text))
                        package = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[syntax.Package.Text]);

                    // if its found load the class / if not class is null
                    if (package != null)
                        staticClass = package.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);
                }

                if (staticClass != null) return BindAccessExpressionSuffix(syntax, new BoundObjectAccessExpression(null, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, package, staticClass, null, null, null), null, staticClass, package);
            }

            // check used packages and see if the class can be found there
            ClassSymbol sClass = null;
            Package.Package pkg = null;

            foreach (string s in _usingPackages)
            {
                pkg = _packageNamespaces.FirstOrDefault(x => x.scope.TryLookupSymbol(syntax.IdentifierToken.Text) != null);

                if(pkg == null && _packageAliases.ContainsKey(s))
                        pkg = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[s]);

                if (pkg != null) break;
            }

            // if the package exists look for the class
            if (pkg != null)
                    sClass = pkg.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);

            // if class found return
            if (sClass != null) return BindAccessExpressionSuffix(syntax, new BoundObjectAccessExpression(null, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, pkg, sClass, null, null, null), null, sClass, pkg);


            // check if enum with that name exists
            var _enum = ParentScope.GetDeclaredEnums().FirstOrDefault(x => x.Name == syntax.IdentifierToken.Text);
            if (_enum != null)
            {
                var bac = new BoundObjectAccessExpression(null, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, null, null, null, null, null);
                bac.Enum = _enum;
                return BindAccessExpressionSuffix(syntax, bac, null, null, null);
            }

            VariableSymbol variable = BindVariableReference(syntax.IdentifierToken);

            if (variable != null)
                return BindVariableAccessExpression(syntax, variable);

            _diagnostics.ReportUnknownAccessSource(syntax.Location);
            return new BoundErrorExpression();
        }

        private BoundExpression BindExpressionAccessExpression(ObjectAccessExpression syntax)
        {
            var exp = BindExpression(syntax.Expression);

            if (exp is BoundErrorExpression)
            {
                _diagnostics.ReportCustomeMessage("expression came out to be an error expression!");
                return exp;
            }

            return BindAccessExpressionSuffix(syntax, new BoundObjectAccessExpression(null, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, packageFromType(exp.Type), null, null, exp,exp.Type), exp.Type, TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == exp.Type.Name).Key, packageFromType(exp.Type));
        }

        private BoundExpression BindVariableAccessExpression(ObjectAccessExpression syntax, VariableSymbol variable)
        {
            return BindAccessExpressionSuffix(syntax, new BoundObjectAccessExpression(variable, syntax.Type, null, ImmutableArray.Create<BoundExpression>(), null, null, null, packageFromType(variable.Type), null, null, null, variable.Type),  variable.Type, variable.Type.isClass ? TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == variable.Type.Name).Key : null, packageFromType(variable.Type));
        }

        private BoundExpression BindAccessExpressionSuffix(ObjectAccessExpression syntax, BoundObjectAccessExpression bac, TypeSymbol type, ClassSymbol classsym, Package.Package package)
        {
            if (classsym == null && bac.Enum == null)
            {
                if (type == null)
                {
                    _diagnostics.ReportClassSymbolNotFound(syntax.Location);
                    return new BoundErrorExpression();
                }
                else if (type.isClass)
                {
                    _diagnostics.ReportClassSymbolNotFound(syntax.Location);
                    return new BoundErrorExpression();
                }
            }

            // check if the value requested is an Enum
            if (bac.Enum != null)
            {
                if (syntax.Type != ObjectAccessExpression.AccessType.Get)
                {
                    _diagnostics.ReportCanOnlyGetFromEnum(syntax.Location, bac.Enum.Name, syntax.Type.ToString());
                    return new BoundErrorExpression();
                }

                var enumEntry = bac.Enum.Values.FirstOrDefault(x => x.Key == syntax.LookingFor.Text).Key;

                if (enumEntry == null)
                {
                    _diagnostics.ReportEnumMemberNotFound(syntax.Location, bac.Enum.Name, syntax.LookingFor.Text);
                    return new BoundErrorExpression();
                }

                var enumTypesymbol = TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == bac.Enum.Name).Value;

                if (enumTypesymbol == null)
                {
                    _diagnostics.ReportCustomeMessage($"Couldnt find Typesymbol for Enum '{bac.Enum.Name}' (if you see this error message, something went wrong pretty badly internally. Please file a bug report)");
                    return new BoundErrorExpression();
                }

                bac.EnumMember = enumEntry;
                bac._type = enumTypesymbol;

                return bac;
            }

            bac.Class = classsym;

            if (type != null)
            if (!type.isClass || type.isClassArray)
            {
                var typeCall = BindTypeCallExpression(syntax, syntax.Call, type);

                if (typeCall is BoundErrorExpression)
                {
                    _diagnostics.ReportTypefunctionNotFound(syntax.Location, syntax.Call.Identifier.Text, type.Name);
                    return new BoundErrorExpression();
                }

                type = typeCall.Type;
                bac._type = type; 
                bac.TypeCall = (BoundTypeCallExpression)typeCall;
                return bac;
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Call)
            {
                Symbol symbol = null;
                if (classsym.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.LookingFor.Text);
                else symbol = classsym.Scope.TryLookupSymbol(syntax.LookingFor.Text, true);

                //check if virtual func for it exists
                if (symbol == null && classsym.ParentSym != null)
                {
                    symbol = classsym.ParentSym.Scope.TryLookupSymbol(syntax.Call.Identifier.Text, true);
                    bac.Class = classsym.ParentSym;
                }

                if (symbol == null || !(symbol is FunctionSymbol))
                {
                    _diagnostics.ReportFunctionNotFoundInObject(syntax.Call.Location, syntax.Call.Identifier.Text, classsym.Name);
                    return new BoundErrorExpression();
                }

                var function = symbol as FunctionSymbol;
                var rtype = function.Type;

                if (function.IsOverride)
                {
                    var virtFunction = classsym.ParentSym.Scope.TryLookupSymbol(function.Name);
                    if (virtFunction == null)
                        _diagnostics.ReportCustomeMessage("override parent function missing! (this is a serious backend error! if this was displayed to you please file a bug report!)");
                    
                    function = virtFunction as FunctionSymbol;
                }

                var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

                foreach (var argument in syntax.Call.Arguments)
                {
                    var boundArgument = BindExpression(argument);
                    boundArguments.Add(boundArgument);
                }

                var arguments = boundArguments.ToImmutable();

                if (function.Parameters.Length != boundArguments.Count)
                {
                    _diagnostics.ReportFunctionInObjectHasDifferentParams(syntax.Call.Location, syntax.Call.Identifier.Text, classsym.Name, boundArguments.Count);
                    return new BoundErrorExpression();
                }

                bac.Function = function;
                bac._type = rtype;
                bac.Arguments = arguments;
                return bac;
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Get)
            {
				Symbol symbol = null;
                if (classsym.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.LookingFor.Text);
                else symbol = classsym.Scope.TryLookupSymbol(syntax.LookingFor.Text, true);

                //check if virtual func for it exists
                if (symbol == null && classsym.ParentSym != null)
                {
                    symbol = classsym.ParentSym.Scope.TryLookupSymbol(syntax.LookingFor.Text, true);

                    bac.Class = classsym.ParentSym;
                }

				Console.WriteLine(symbol);

                if (symbol == null || !(symbol is VariableSymbol))
                {
                    _diagnostics.ReportVariableNotFoundInObject(syntax.LookingFor.Location, syntax.LookingFor.Text, classsym.Name);
                    return new BoundErrorExpression();
                }


                var property = symbol as VariableSymbol;
                var rtype = property.Type;

                bac.Property = property;
                bac._type = rtype;
                return bac;
            }

            if (syntax.Type == ObjectAccessExpression.AccessType.Set)
            {
                Symbol symbol = null;
                if (classsym.Name == "Main") symbol = ParentScope.TryLookupSymbol(syntax.LookingFor.Text);
                else symbol = classsym.Scope.TryLookupSymbol(syntax.LookingFor.Text, true);

                //check if virtual func for it exists
                if (symbol == null && classsym.ParentSym != null)
                {
                    symbol = classsym.ParentSym.Scope.TryLookupSymbol(syntax.LookingFor.Text, true);
                    bac.Class = classsym.ParentSym;
                }

                if (symbol == null || !(symbol is VariableSymbol))
                {
                    _diagnostics.ReportVariableNotFoundInObject(syntax.LookingFor.Location, syntax.LookingFor.Text, classsym.Name);
                    return new BoundErrorExpression();
                }

                bac.Property = symbol as VariableSymbol;
                var value = BindExpression(syntax.Value);
                bac.Value = BindConversion(syntax.Value.Location, value, bac.Property.Type);
                bac._type = TypeSymbol.Void;
                
                return bac;
            }

            return bac;
        }

        private BoundExpression BindTernaryExpression(TernaryExpressionSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition);
            var left = BindExpression(syntax.Left);
            var right = BindExpression(syntax.Right);

            if (condition is BoundErrorExpression || left is BoundErrorExpression || right is BoundErrorExpression)
                return new BoundErrorExpression();

            if (condition.Type != TypeSymbol.Bool)
            {
                _diagnostics.ReportWrongConditionType(syntax.Condition.Location, condition.Type.Name);
                return new BoundErrorExpression();
            }

            if (left.Type != right.Type)
            {
                _diagnostics.ReportTernaryLeftAndRightTypesDontMatch(syntax.Location, left.Type.Name, right.Type.Name);
                return new BoundErrorExpression();
            }

            return new BoundTernaryExpression(condition, left, right);
        }
        
        private BoundExpression BindLambdaExpression(LambdaExpressionSyntax syntax)
        {
            if (_function != null && _function.Name == "anon")
            {
                _diagnostics.ReportLambdaInLambda(syntax.Location);
                return new BoundErrorExpression();
            }

            var prev = _function;
            _function = new FunctionSymbol("anon", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
            var boundBlock = (BoundBlockStatement)BindBlockStatement((BlockStatementSyntax)syntax.Block);
            var loweredBody = (BoundBlockStatement)Lowerer.Lower(_function, boundBlock);

            _function = prev;

            return new BoundLambdaExpression(loweredBody);
        }

        private BoundExpression BindIsExpression(IsExpressionSyntax syntax)
        {
            var left = BindExpression(syntax.Left);
            var type = LookupType(syntax.Type.Text);

            if (left is BoundErrorExpression)
                return new BoundErrorExpression();

            if (type == null)
            {
                _diagnostics.ReportUndefinedType(syntax.Type.Location, syntax.Type.Text);
                return new BoundErrorExpression();
            }

            if (!type.isClass || (type.isClass && TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == type.Name).Key.ParentSym == null))
            {
                _diagnostics.ReportInstanceTestTypeNeedsToBeInheratingClass(syntax.Type.Location, syntax.Type.Text);
                return new BoundErrorExpression();
            }

            var typeClass = TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == type.Name).Key;

            if (!left.Type.isClass ||
                ((left.Type.isClass && !TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == left.Type.Name).Key.IsAbstract) &&
                (left.Type.isClass && TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == left.Type.Name).Key.ParentSym == null)))
            {
                _diagnostics.ReportInstanceTestTypeNeedsToBeAbstractOrInheratingClass(syntax.Left.Location, left.Type.Name);
                return new BoundErrorExpression();
            }

            var leftClass = TypeSymbol.Class.FirstOrDefault(x => x.Value.Name == left.Type.Name).Key;

            if ((!leftClass.IsAbstract && typeClass.ParentSym.Name != leftClass.ParentSym.Name) ||
               (leftClass.IsAbstract && typeClass.ParentSym.Name != leftClass.Name))
            {
               _diagnostics.ReportInstanceTestTypeNeedsToInherateFromSameClass(syntax.Location, type.Name, left.Type.Name);
               return new BoundErrorExpression();
            }

            return new BoundIsExpression(left, type);
        }

        private BoundExpression BindArrayCreationExpression(ArrayCreationSyntax syntax)
        {
            var type = LookupType(syntax.Type.Text);
            var length = BindExpressionInternal(syntax.Length);

            if (length is BoundErrorExpression)
            {
                _diagnostics.ReportUnknownArrayLength(syntax.Length.Location);
                return new BoundErrorExpression();
            }

            var arrType = TypeToArray(type);

            return new BoundArrayCreationExpression(type, length, arrType);
        }

        private BoundExpression BindArrayLiteralExpression(ArrayLiteralExpressionSyntax syntax)
        {
            var type = LookupType(syntax.Type.Text);
            var arrType = TypeToArray(type);

            List<BoundExpression> values = new List<BoundExpression>();

            foreach(var exp in syntax.Values)
            {
                var boundExp = BindExpression(exp);
                values.Add(boundExp);

                if (boundExp.Type != type)
                {
                    _diagnostics.ReportElementTypeDoesNotMatchArrayType(exp.Location, boundExp.Type.Name, type.Name);
                    return new BoundErrorExpression();
                }
            }

            return new BoundArrayLiteralExpression(type, arrType, values.ToArray());
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            ClassSymbol _class = null;
            var variable = BindVariableReference(syntax.IdentifierToken, true);
            if (variable == null && inClass != null && inClass.ParentSym != null)
            {
                var baseVar = inClass.ParentSym.Scope.TryLookupSymbol(syntax.IdentifierToken.Text);
                if (baseVar != null && baseVar is VariableSymbol v)
                    variable = v;

                _class = inClass.ParentSym;
            }

            if (variable == null)
            {
                _diagnostics.ReportUndefinedVariable(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression();
            }

            if (variable.IsFunctional && inClass != null && inClass.ParentSym != null)
                _class = inClass.ParentSym;

            if (variable.IsReadOnly)
                _diagnostics.ReportCannotAssign(syntax.EqualsToken.Location, name);

            if (syntax.isArray)
            {
                var cExpression = BindConversion(syntax.Expression.Location, boundExpression, ArrayToType(variable.Type));
                var boundIndex = BindExpression(syntax.Index);
                return new BoundAssignmentExpression(variable, cExpression, boundIndex, _class);
            }

            var convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type);

            return new BoundAssignmentExpression(variable, convertedExpression, _class);
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

            if (function.Parameters.Length > 0)
            {
                _diagnostics.ReportCantThreadFunctionWithArgs(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            return new BoundThreadCreateExpression(function);
        }

        private BoundExpression BindActionCreateExpression(ActionCreationSyntax syntax)
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

            if (function.Parameters.Length > 0)
            {
                _diagnostics.ReportCantActionFunctionWithArgs(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (function.Type != TypeSymbol.Void)
            {
                _diagnostics.ReportCanOnlyActionVoids(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            return new BoundActionCreateExpression(function);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            if (syntax == null)
            {
                _diagnostics.ReportCustomeMessage("Call syntax was null (this message should never be shown. if it was please file a bug report)");
                return new BoundErrorExpression();
            }

            var _symbol = _scope.TryLookupSymbol(syntax.Identifier.Text);
            
            if (_symbol == null)
            {
                foreach (string s in _usingPackages)
                {
                    var pkg = _packageNamespaces.FirstOrDefault(x => x.name == s);

                    if(pkg == null && _packageAliases.ContainsKey(s))
                        pkg = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[s]);

                    if (pkg != null && pkg.scope.TryLookupSymbol(syntax.Identifier.Text) != null)
                    {
                        syntax.Namespace = s;
                        break;
                    }
                }
            }

            Package.Package pack = null;

            if (syntax.Namespace != "")
            {
                pack = _packageNamespaces.FirstOrDefault(x => x.name == syntax.Namespace);
                if (pack == null && _packageAliases.ContainsKey(syntax.Namespace))
                    pack = _packageNamespaces.FirstOrDefault(x => x.name == _packageAliases[syntax.Namespace]);
                

                if (pack == null)
                {
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

            var symbol = (syntax.Namespace == "" ? _scope : pack.scope).TryLookupSymbol(syntax.Identifier.Text);
            ClassSymbol _class = null;
            if (symbol == null && inClass != null && inClass.ParentSym != null)
            {
                var baseFunc = inClass.ParentSym.Scope.TryLookupSymbol(syntax.Identifier.Text);

                if (baseFunc != null && baseFunc is FunctionSymbol)
                {
                    symbol = baseFunc;
                }

                _class = inClass.ParentSym;
            }

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

            if ((function.IsOverride || function.IsVirtual )&& inClass != null && inClass.ParentSym != null)
            {
                _class = inClass.ParentSym;
            }

            if ((function.IsOverride || function.IsVirtual )&& inClass != null && inClass.IsAbstract)
            {
                _class = inClass;
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

            // Console.WriteLine("fnc: " + function.Name);
            // Console.WriteLine("ovr: " + function.IsOverride);
            // Console.WriteLine("virt: " + function.IsVirtual);
            // Console.WriteLine("inClass: " + inClass);
            // Console.WriteLine("parent: " + inClass.ParentSym);

            return new BoundCallExpression(function, boundArguments.ToImmutable(), syntax.Namespace, _class);
        }

        private BoundExpression BindTypeCallExpression(ObjectAccessExpression stmt, CallExpressionSyntax syntax, TypeSymbol etype)
        {
            if (syntax == null)
            {
                _diagnostics.ReportCustomeMessage("Call syntax was null (this message should never be shown. if it was please file a bug report)");
                return new BoundErrorExpression();
            }

            var tfSyms = BuiltinFunctions.GetAllTypeFunctions().Where(x => x.Name == syntax.Identifier.Text).ToList();
            var tfsym = tfSyms.FirstOrDefault(x => x.Childtype == etype);

            if (tfsym == null && (etype.isArray || etype.isClassArray))
            {
                tfsym = tfSyms.FirstOrDefault(x => x.Childtype == TypeSymbol.AnyArr);
            }

            if (tfsym == null)
            {
                //should already be reported in BindAccessExpressionSuffix
                //_diagnostics.ReportTypefunctionNotFound(syntax.Location, syntax.Identifier.Text, etype.Name);
                return new BoundErrorExpression();
            }

            if (syntax.Arguments.Count == 1 && LookupType(syntax.Identifier.Text) is TypeSymbol type)
                return BindConversion(syntax.Arguments[0], type, allowExplicit: true);

            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArguments.Add(boundArgument);
            }

            if (syntax.Arguments.Count != tfsym.Parameters.Length)
            {
                TextSpan span;
                if (syntax.Arguments.Count > tfsym.Parameters.Length)
                {
                    SyntaxNode firstExceedingNode;
                    if (tfsym.Parameters.Length > 0)
                        firstExceedingNode = syntax.Arguments.GetSeparator(tfsym.Parameters.Length - 1);
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
                _diagnostics.ReportWrongArgumentCount(location, tfsym.Name, tfsym.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            for (var i = 0; i < syntax.Arguments.Count; i++)
            {
                var argumentLocation = syntax.Arguments[i].Location;
                var argument = boundArguments[i];
                var parameter = tfsym.Parameters[i];
                boundArguments[i] = BindConversion(argumentLocation, argument, parameter.Type);
            }

            // resolve array type if TypeSymbol.ArrBase was used
            return new BoundTypeCallExpression(tfsym, boundArguments.ToImmutable(), syntax.Namespace, tfsym.Type == TypeSymbol.ArrBase ? ArrayToType(etype) : tfsym.Type);
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

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol type, SyntaxKind syntax, bool isVirtual, bool isOverride)
        {
            var name = identifier.Text ?? "?";
            var declare = !identifier.IsMissing;
            var variable = syntax == SyntaxKind.SetKeyword ?
                                isVirtual || isOverride ? 
                                     (VariableSymbol)new FunctionalVariableSymbol(name, isReadOnly, type, isVirtual, isOverride, identifier.Location)
                                    :(VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, type, identifier.Location)
                                : new LocalVariableSymbol(name, isReadOnly, type, identifier.Location);

            //Console.WriteLine($"VAR: {name} | {declare} | {variable} | {(variable.IsGlobal ? getClassScope() : _scope).Name}");

            if (inClass == null && isVirtual)
                _diagnostics.ReportVirtualVarInMain(identifier.Location);

            if (inClass != null && !inClass.IsAbstract && isVirtual)
                _diagnostics.ReportCantUseVirtVarInNormalClass(identifier.Location);

            if (inClass == null && isOverride)
                _diagnostics.ReportOverrideVarInMain(identifier.Location);

            if (inClass != null && inClass.ParentSym == null && isOverride)
                _diagnostics.ReportCantUseOvrVarInNormalClass(identifier.Location);

            if (inClass != null && inClass.ParentSym != null && isOverride)
            {   
                var sym = inClass.ParentSym.Scope.TryLookupSymbol(identifier.Text);

                if (sym == null || !(sym is VariableSymbol))
                    _diagnostics.ReportCantFindVarToOverride(identifier.Location, identifier.Text);
            }

            if (declare &&  variable.IsGlobal ? !getClassScope().TryDeclareVariable(variable) : !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);

            return variable;
        }

        private VariableSymbol BindVariableReference(SyntaxToken identifierToken, bool allowedToFail = false)
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
                    if (!allowedToFail)
                        _diagnostics.ReportUndefinedVariable(identifierToken.Location, name);
                    return null;

                default:
                    if (!allowedToFail)
                        _diagnostics.ReportNotAVariable(identifierToken.Location, name);
                    return null;
            }
        }

        private Package.Package packageFromType(TypeSymbol type)
        {
            if (type != null && (type.isArray || type.isClassArray))
                return resolvePackageType(ArrayToType(type));
            else
                return resolvePackageType(type);
        }

        private Package.Package resolvePackageType(TypeSymbol type)
        {
            foreach (var package in _packageNamespaces)
            {
                var _class = package.scope.GetDeclaredClasses().FirstOrDefault(x => x.Name == type.Name);

                if (_class != null)
                    return package;
            }

            return null;
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
                case "byte":
                    return TypeSymbol.Byte;
                case "string":
                    return TypeSymbol.String;
                case "float":
                    return TypeSymbol.Float;
                case "thread":
                    return TypeSymbol.Thread;
                case "action":
                    return TypeSymbol.Action;

                case "anyArr":
                    return TypeSymbol.AnyArr;
                case "boolArr":
                    return TypeSymbol.BoolArr;
                case "intArr":
                    return TypeSymbol.IntArr;
                case "byteArr":
                    return TypeSymbol.ByteArr;
                case "stringArr":
                    return TypeSymbol.StringArr;
                case "floatArr":
                    return TypeSymbol.FloatArr;
                case "threadArr":
                    return TypeSymbol.ThreadArr;
                case "actionArr":
                    return TypeSymbol.ActionArr;
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
            else if (type == TypeSymbol.Byte)
                return TypeSymbol.ByteArr;
            else if (type == TypeSymbol.String)
                return TypeSymbol.StringArr;
            else if (type == TypeSymbol.Float)
                return TypeSymbol.FloatArr;
            else if (type == TypeSymbol.Thread)
                return TypeSymbol.ThreadArr;
            else if (type == TypeSymbol.Action)
                return TypeSymbol.ActionArr;
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
            else if (type == TypeSymbol.ByteArr)
                return TypeSymbol.Byte;
            else if (type == TypeSymbol.StringArr)
                return TypeSymbol.String;
            else if (type == TypeSymbol.FloatArr)
                return TypeSymbol.Float;
            else if (type == TypeSymbol.ThreadArr)
                return TypeSymbol.Thread;
            else if (type == TypeSymbol.ActionArr)
                return TypeSymbol.Action;
            else if (type.isClass)
                return TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == type.Name.Substring(0, type.Name.Length - 3)).Value;

            return type;
        }
    }
}
