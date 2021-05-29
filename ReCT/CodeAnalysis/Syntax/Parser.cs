using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Text;

namespace ReCT.CodeAnalysis.Syntax
{
    internal sealed class Parser
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SyntaxTree syntaxTree)
        {
            var tokens = new List<SyntaxToken>();

            var lexer = new Lexer(syntaxTree);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind != SyntaxKind.WhitespaceToken &&
                    token.Kind != SyntaxKind.BadToken)
                {
                    tokens.Add(token);
                }
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private SyntaxToken Peek(int offset)
        {
            var index = _position + offset;

            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            var current = Current;
            _position++;
            return current;
        }

        private void Rewind(SyntaxToken token)
        {
            while(Current != token)
            {
                _position--;
            }
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree, kind, Current.Position, null, null);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var members = ParseMembers();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;

                var member = ParseMember();
                members.Add(member);

                if (Current == startToken)
                    NextToken();
            }

            return members.ToImmutable();
        }
        private ImmutableArray<MemberSyntax> ParseMembersInternal()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();

            MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken && Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;

                var member = ParseMember();
                members.Add(member);

                if (Current == startToken)
                    NextToken();
            }

            if (Current.Kind == SyntaxKind.CloseBraceToken)
                MatchToken(SyntaxKind.CloseBraceToken);

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember(List<SyntaxToken> prefixes = null)
        {   
            prefixes = prefixes ?? new List<SyntaxToken>();

            if (Current.Kind == SyntaxKind.EnumKeyword)
                if (prefixes.Count == 0)
                    return ParseEnumDeclaration();
                else {
                    foreach (var pre in prefixes)
                    _diagnostics.ReportMemberCantUseModifier(pre.Location, "Enum", pre.Text);
                    return null;
                }

            if (Current.Kind == SyntaxKind.SetKeyword && Peek(1).Kind != SyntaxKind.IdentifierToken)
            {
                prefixes.Add(MatchToken(SyntaxKind.SetKeyword));
                return ParseMember(prefixes);
            }

            if (Current.Kind == SyntaxKind.IncKeyword) {
                prefixes.Add(MatchToken(SyntaxKind.IncKeyword));
                return ParseMember(prefixes);
            }

            if (Current.Kind == SyntaxKind.AbstractKeyword) {
                prefixes.Add(MatchToken(SyntaxKind.AbstractKeyword));
                return ParseMember(prefixes);
            }

            if (Current.Kind == SyntaxKind.VirtualKeyword) {
                prefixes.Add(MatchToken(SyntaxKind.VirtualKeyword));
                return ParseMember(prefixes);
            }

            if (Current.Kind == SyntaxKind.SerializableKeyword) {
                prefixes.Add(MatchToken(SyntaxKind.SerializableKeyword));
                return ParseMember(prefixes);
            }

            if (Current.Kind == SyntaxKind.FunctionKeyword)
                return ParseFunctionDeclaration(prefixes);

            if (Current.Kind == SyntaxKind.ClassKeyword)
                return ParseClassDeclaration(prefixes);

            if (Current.Kind == SyntaxKind.IdentifierToken && prefixes.Count != 0) {
                foreach(var prefix in prefixes)
                    _diagnostics.ReportPrefixedWithUnknownTarget(prefix.Location);
                return null;
            }

            return ParseGlobalStatement(prefixes);
        }

        private MemberSyntax ParseFunctionDeclaration(List<SyntaxToken> prefixes)
        {
            var isPublic = false;
            var isVirtual = false;

            foreach(SyntaxToken s in prefixes)
            {
                switch(s.Kind)
                {
                    case SyntaxKind.SetKeyword:
                        if (!isPublic)
                            isPublic = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    case SyntaxKind.VirtualKeyword:
                        if (!isVirtual)
                            isVirtual = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    case SyntaxKind.AbstractKeyword:
                        _diagnostics.ReportFunctionCantBeAbstract(s.Location);
                        break;
                    default:
                        _diagnostics.ReportMemberCantUseModifier(s.Location, "Class", s.Text);
                        break;
                }
            }

            var functionKeyword = MatchToken(SyntaxKind.FunctionKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseParameterList();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            var type = ParseOptionalTypeClause();
            var body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, functionKeyword, identifier, openParenthesisToken, parameters, closeParenthesisToken, type, body, isPublic, isVirtual);
        }

        private MemberSyntax ParseClassDeclaration(List<SyntaxToken> prefixes)
        {
            var isStatic = false;
            var isIncluded = false;
            var isAbstract = false;
            var isSerializable = false;

            foreach(SyntaxToken s in prefixes)
            {
                switch(s.Kind)
                {
                    case SyntaxKind.SetKeyword:
                        if (!isStatic)
                            isStatic = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    case SyntaxKind.IncKeyword:
                        if (!isIncluded)
                            isIncluded = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    case SyntaxKind.AbstractKeyword:
                        if (!isAbstract)
                            isAbstract = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    case SyntaxKind.SerializableKeyword:
                        if (!isSerializable)
                            isSerializable = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    default:
                        _diagnostics.ReportMemberCantUseModifier(s.Location, "Class", s.Text);
                        break;
                }
            }

            var classKeyword = MatchToken(SyntaxKind.ClassKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var members = ParseMembersInternal();
            return new ClassDeclarationSyntax(_syntaxTree, classKeyword, identifier, members, isStatic, isIncluded, isAbstract, isSerializable);
        }

        private MemberSyntax ParseEnumDeclaration()
        {
            var enumKeyword = MatchToken(SyntaxKind.EnumKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            MatchToken(SyntaxKind.OpenBraceToken);
            
            List<SyntaxToken> names = new List<SyntaxToken>();
            Dictionary<SyntaxToken, ExpressionSyntax> values = new Dictionary<SyntaxToken, ExpressionSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var name = MatchToken(SyntaxKind.IdentifierToken);
                names.Add(name);

                if (Current.Kind == SyntaxKind.AssignToken)
                {
                    MatchToken(SyntaxKind.AssignToken);
                    values.Add(name, ParseNumberLiteral());
                }

                if (Current.Kind != SyntaxKind.CommaToken) break;
                MatchToken(SyntaxKind.CommaToken);
            }

            MatchToken(SyntaxKind.CloseBraceToken);

            return new EnumDeclarationSyntax(_syntaxTree, identifier, names.ToArray(), values);
        }

        private ExpressionSyntax ParseThreadCreation()
        {
            var threadKeyword = MatchToken(SyntaxKind.ThreadKeyword);
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ThreadCreationSyntax(_syntaxTree, identifier);
        }

        private ExpressionSyntax ParseActionCreation()
        {
            var actionKeyword = MatchToken(SyntaxKind.ActionKeyword);
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ActionCreationSyntax(_syntaxTree, identifier);
        }

        private ExpressionSyntax ParseArrayCreation()
        {
            var makeKeyword = MatchToken(SyntaxKind.MakeKeyword);
            SyntaxToken package = null;

            if (Peek(1).Kind == SyntaxKind.NamespaceToken)
            {
                package = MatchToken(SyntaxKind.IdentifierToken);
                MatchToken(SyntaxKind.NamespaceToken);
            }

            var type = MatchToken(SyntaxKind.IdentifierToken);
            var arrayKeyword = MatchToken(SyntaxKind.ArrayKeyword);

            if (Current.Kind == SyntaxKind.OpenBraceToken)
                return ParseArrayLiteral(package, type);

            MatchToken(SyntaxKind.OpenParenthesisToken);
            var length = ParseExpression();
            MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ArrayCreationSyntax(_syntaxTree, type, length, package);
        }

        private ExpressionSyntax ParseArrayLiteral(SyntaxToken package, SyntaxToken type)
        {
            List<ExpressionSyntax> values = new List<ExpressionSyntax>();

            MatchToken(SyntaxKind.OpenBraceToken);

            while(Current.Kind != SyntaxKind.CloseBraceToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                values.Add(ParseExpression());

                if (Current.Kind != SyntaxKind.CloseBraceToken)
                    MatchToken(SyntaxKind.CommaToken);
            }

            MatchToken(SyntaxKind.CloseBraceToken);

            return new ArrayLiteralExpressionSyntax(_syntaxTree, type, package, values.ToArray());
        }

        private ExpressionSyntax ParseArrayExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            MatchToken(SyntaxKind.OpenBracketToken);
            var index = ParseExpression();
            MatchToken(SyntaxKind.CloseBracketToken);

            return new NameExpressionSyntax(_syntaxTree, identifierToken, index);
        }

        private ExpressionSyntax ParseObjectCreation()
        {
            var makeKeyword = MatchToken(SyntaxKind.MakeKeyword);
            SyntaxToken package = null;

            if (Peek(1).Kind == SyntaxKind.NamespaceToken)
            {
                package = NextToken();
                MatchToken(SyntaxKind.NamespaceToken);
            }

            var type = MatchToken(SyntaxKind.IdentifierToken);
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var args = ParseArguments();
            MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ObjectCreationSyntax(_syntaxTree, type, args, package);
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextParameter = true;
            while (parseNextParameter &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = ParseParameter();
                nodesAndSeparators.Add(parameter);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeparatedSyntaxList<ParameterSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ParameterSyntax ParseParameter()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);

            if (Current.Kind == SyntaxKind.AccessToken)
                MatchToken(SyntaxKind.AccessToken);

            var type = ParseTypeClause();
            return new ParameterSyntax(_syntaxTree, identifier, type);
        }

        private MemberSyntax ParseGlobalStatement(List<SyntaxToken> prefixes)
        {
            var statement = ParseStatement(prefixes);
            return new GlobalStatementSyntax(_syntaxTree, statement);
        }

        private StatementSyntax ParseStatement(List<SyntaxToken> prefixes = null)
        {
            prefixes = prefixes ?? new List<SyntaxToken>();

            if (prefixes.Count != 0 && (Current.Kind != SyntaxKind.SetKeyword && Current.Kind != SyntaxKind.VarKeyword))
                _diagnostics.ReportPrefixedWithUnknownTarget(prefixes[0].Location);

            switch (Current.Kind)
            {
                case SyntaxKind.PackageKeyword:
                    return ParsePackageStatement();
                case SyntaxKind.NamespaceKeyword:
                    return ParseNamespaceStatement();
                case SyntaxKind.AliasKeyword:
                    return ParseAliasStatement();
                case SyntaxKind.TypeKeyword:
                    return ParseTypeStatement();
                case SyntaxKind.UseKeyword:
                    return ParseUseStatement();
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.SetKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclaration(prefixes);
                case SyntaxKind.IfKeyword:
                    return ParseIfStatement();
                case SyntaxKind.WhileKeyword:
                    return ParseWhileStatement();
                case SyntaxKind.DoKeyword:
                    return ParseDoWhileStatement();
                case SyntaxKind.ForKeyword:
                    return ParseForStatement();
                case SyntaxKind.FromKeyword:
                    return ParseFromToStatement();
                case SyntaxKind.BreakKeyword:
                    return ParseBreakStatement();
                case SyntaxKind.ContinueKeyword:
                    return ParseContinueStatement();
                case SyntaxKind.ReturnKeyword:
                    return ParseReturnStatement();
                case SyntaxKind.TryKeyword:
                    return ParseTryCatchStatement();
                case SyntaxKind.AbstractKeyword:
                case SyntaxKind.SerializableKeyword:
                case SyntaxKind.VirtualKeyword:
                case SyntaxKind.IncKeyword:
                    _diagnostics.ReportModifierInFunction(Current.Location, Current.Text);
                    return null;
                default:
                    return ParseExpressionStatement();
            }
        }

        private StatementSyntax ParseUseStatement()
        {
            var keyword = MatchToken(SyntaxKind.UseKeyword);
            var name = NextToken();

            return new UseStatementSyntax(_syntaxTree, keyword, name);
        }

        private StatementSyntax ParseAliasStatement()
        {
            var keyword = MatchToken(SyntaxKind.AliasKeyword);
            var mapthis = NextToken();
            var tothis = NextToken();

            return new AliasStatementSyntax(_syntaxTree, keyword, mapthis, tothis);
        }

        private StatementSyntax ParseNamespaceStatement()
        {
            var keyword = MatchToken(SyntaxKind.NamespaceKeyword);
            var name = NextToken();

            return new NamespaceStatementSyntax(_syntaxTree, keyword, name);
        }
        private StatementSyntax ParseTypeStatement()
        {
            var keyword = MatchToken(SyntaxKind.TypeKeyword);
            var name = NextToken();

            return new TypeStatementSyntax(_syntaxTree, keyword, name);
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;

                var statement = ParseStatement();
                statements.Add(statement);
                if (Current == startToken)
                    NextToken();
            }

            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private StatementSyntax ParseVariableDeclaration(List<SyntaxToken> prefixes = null)
        {
            prefixes = prefixes ?? new List<SyntaxToken>();

            var isVirtual = false;
            var expected = Current.Kind == SyntaxKind.SetKeyword ? SyntaxKind.SetKeyword : SyntaxKind.VarKeyword;
            var keyword = MatchToken(expected);

            foreach(SyntaxToken s in prefixes)
            {
                switch(s.Kind)
                {
                    case SyntaxKind.VirtualKeyword:
                        if (keyword.Kind == SyntaxKind.VarKeyword) {
                            _diagnostics.ReportLocalVariableCantBeVirtual(s.Location);
                            break;
                        }

                        if (!isVirtual)
                            isVirtual = true;
                        else
                            _diagnostics.ReportMemberAlreadyRecieved(s.Location, s.Text);
                        break;
                    default:
                        _diagnostics.ReportMemberCantUseModifier(s.Location, "Variable", s.Text);
                        break;
                }
            }

            var typeClause = (TypeClauseSyntax)null;

            if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.IdentifierToken)
                typeClause = ParseOptionalTypeClause();

            var identifier = MatchToken(SyntaxKind.IdentifierToken);

            if (Current.Kind != SyntaxKind.AssignToken)
                return new VariableDeclarationSyntax(_syntaxTree, keyword, identifier, typeClause, null, null, null, isVirtual);

            var equals = MatchToken(SyntaxKind.AssignToken);
            var initializer = ParseExpression();

            return new VariableDeclarationSyntax(_syntaxTree, keyword, identifier, typeClause, equals, initializer, null,isVirtual);
        }

        private TypeClauseSyntax ParseOptionalTypeClause()
        {
            if (Current.Kind != SyntaxKind.IdentifierToken && Current.Kind != SyntaxKind.AccessToken)
                return null;

            return ParseTypeClause();
        }

        private TypeClauseSyntax ParseTypeClause()
        {
            if (Current.Kind == SyntaxKind.AccessToken)
                MatchToken(SyntaxKind.AccessToken);

            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            return new TypeClauseSyntax(_syntaxTree, identifier);
        }

        private StatementSyntax ParseIfStatement()
        {
            var keyword = MatchToken(SyntaxKind.IfKeyword);
            var condition = ParseExpression();
            var statement = ParseStatement();
            var elseClause = ParseElseClause();
            return new IfStatementSyntax(_syntaxTree, keyword, condition, statement, elseClause);
        }

        private StatementSyntax ParsePackageStatement()
        {
            var keyword = MatchToken(SyntaxKind.PackageKeyword);

            var isDll = Current.Kind == SyntaxKind.DllKeyword;
            if (isDll) MatchToken(SyntaxKind.DllKeyword);

            var package = NextToken();
            return new PackageStatementSyntax(_syntaxTree, keyword, package, isDll);
        }

        private StatementSyntax ParseTryCatchStatement()
        {
            var trykeyword = MatchToken(SyntaxKind.TryKeyword);
            var statement = ParseStatement();
            var catchKeyword = MatchToken(SyntaxKind.CatchKeyword);
            var catchStatement = ParseStatement();
            return new TryCatchStatementSyntax(_syntaxTree, trykeyword, statement, catchKeyword, catchStatement);
        }

        private ElseClauseSyntax ParseElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
                return null;

            var keyword = NextToken();
            var statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private StatementSyntax ParseWhileStatement()
        {
            var keyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            var body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree, keyword, condition, body);
        }

        private StatementSyntax ParseDoWhileStatement()
        {
            var doKeyword = MatchToken(SyntaxKind.DoKeyword);
            var body = ParseStatement();
            var whileKeyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, condition);
        }

        private StatementSyntax ParseForStatement()
        {
            var keyword = MatchToken(SyntaxKind.ForKeyword);
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var variable = ParseVariableDeclaration();
            var condition = ParseExpression();
            var action = ParseExpression();
            MatchToken(SyntaxKind.CloseParenthesisToken);
            var body = ParseStatement();
            return new ForStatementSyntax(_syntaxTree, keyword, variable, condition, action, body);
        }

        private StatementSyntax ParseFromToStatement()
        {
            var keyword = MatchToken(SyntaxKind.FromKeyword);
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equalsToken = MatchToken(SyntaxKind.AssignToken);
            var lowerBound = ParseExpression();
            MatchToken(SyntaxKind.CloseParenthesisToken);
            var toKeyword = MatchToken(SyntaxKind.ToKeyword);
            var upperBound = ParseExpression();
            var body = ParseStatement();
            return new FromToStatementSyntax(_syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, body);
        }

        private StatementSyntax ParseBreakStatement()
        {
            var keyword = MatchToken(SyntaxKind.BreakKeyword);
            return new BreakStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseContinueStatement()
        {
            var keyword = MatchToken(SyntaxKind.ContinueKeyword);
            return new ContinueStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseReturnStatement()
        {
            var keyword = MatchToken(SyntaxKind.ReturnKeyword);
            var keywordLine = _text.GetLineIndex(keyword.Span.Start);
            var currentLine = _text.GetLineIndex(Current.Span.Start);
            var isEof = Current.Kind == SyntaxKind.EndOfFileToken;
            var sameLine = !isEof && keywordLine == currentLine;
            var expression = sameLine ? ParseExpression() : null;
            return new ReturnStatementSyntax(_syntaxTree, keyword, expression);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            var expression = ParseExpression();
            return new ExpressionStatementSyntax(_syntaxTree, expression);
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                Peek(1).Kind == SyntaxKind.OpenBracketToken)
            {
                var identifierToken = NextToken();
                MatchToken(SyntaxKind.OpenBracketToken);
                var index = ParseExpression();
                MatchToken(SyntaxKind.CloseBracketToken);

                if (Peek(0).Kind == SyntaxKind.AssignToken)
                {
                    var operatorToken = NextToken();
                    var right = ParseExpression();
                    return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, operatorToken, right, index);
                }

                // if it didnt return that means the user actually just wanted the value
                Rewind(identifierToken);
            }

            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                Peek(1).Kind == SyntaxKind.AssignToken)
            {
                var identifierToken = NextToken();
                var operatorToken = NextToken();
                var right = ParseAssignmentExpression();
                return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, operatorToken, right);
            }

            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                Peek(1).Kind == SyntaxKind.EditVariableToken)
            {
                var identifierToken = NextToken();
                var operatorToken = NextToken();
                var right = ParseAssignmentExpression();

                var editor = new BinaryExpressionSyntax(_syntaxTree, new NameExpressionSyntax(_syntaxTree, identifierToken), new SyntaxToken(_syntaxTree, (SyntaxKind)operatorToken.Value, 0, SyntaxFacts.GetText((SyntaxKind)operatorToken.Value), null), right);

                return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, new SyntaxToken(_syntaxTree, SyntaxKind.EqualsToken, 0, "<-", null), editor);
            }

            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                Peek(1).Kind == SyntaxKind.SingleEditVariableToken)
            {
                var identifierToken = NextToken();
                var operatorToken = NextToken();

                var editor = new BinaryExpressionSyntax(_syntaxTree, new NameExpressionSyntax(_syntaxTree, identifierToken), new SyntaxToken(_syntaxTree, (SyntaxKind)operatorToken.Value, 0, SyntaxFacts.GetText((SyntaxKind)operatorToken.Value), null), new LiteralExpressionSyntax(_syntaxTree, new SyntaxToken(_syntaxTree, SyntaxKind.NumberToken, 0, "1", 1)));

                return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, new SyntaxToken(_syntaxTree, SyntaxKind.EqualsToken, 0, "<-", null), editor);
            }

            return ParseBinaryExpression();
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0, bool inUnary = false)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseBinaryExpression(unaryOperatorPrecedence, true);
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                var operatorToken = NextToken();
                var right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
            }

            if (Current.Kind == SyntaxKind.AccessToken && !inUnary) return ParseExpressionAccessExpression(left);
            if (Current.Kind == SyntaxKind.QuestionMarkToken && !inUnary && parentPrecedence == 0) return ParseTernaryExpression(left);

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.FalseKeyword:
                case SyntaxKind.TrueKeyword:
                    return ParseBooleanLiteral();

                case SyntaxKind.NumberToken:
                    return ParseNumberLiteral();

                case SyntaxKind.NullKeyword:
                    return ParseNullLiteral();
                
                case SyntaxKind.StringToken:
                    return ParseStringLiteral();
                case SyntaxKind.ThreadKeyword:
                    return ParseThreadCreation();
                case SyntaxKind.ActionKeyword:
                    return ParseActionCreation();
                case SyntaxKind.MakeKeyword when Peek(2).Kind == SyntaxKind.ArrayKeyword || (Peek(4).Kind == SyntaxKind.ArrayKeyword && Peek(2).Kind == SyntaxKind.NamespaceToken):
                    return ParseArrayCreation();
                case SyntaxKind.MakeKeyword when Peek(2).Kind == SyntaxKind.OpenParenthesisToken || (Peek(4).Kind == SyntaxKind.OpenParenthesisToken && Peek(2).Kind == SyntaxKind.NamespaceToken):
                    return ParseObjectCreation();
                case SyntaxKind.AccessKeyword:
                    return ParseObjectAccessExpression();
                case SyntaxKind.IdentifierToken:
                default:
                    return ParseNameOrCallExpression();
            }
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            var left = MatchToken(SyntaxKind.OpenParenthesisToken);
            var expression = ParseExpression();
            var right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            var isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            var keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) : MatchToken(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private ExpressionSyntax ParseNumberLiteral()
        {
            var numberToken = MatchToken(SyntaxKind.NumberToken);
            return new LiteralExpressionSyntax(_syntaxTree, numberToken);
        }

        private ExpressionSyntax ParseNullLiteral()
        {
            var nullToken = MatchToken(SyntaxKind.NullKeyword);
            return new LiteralExpressionSyntax(_syntaxTree, nullToken);
        }
        
        private ExpressionSyntax ParseStringLiteral()
        {
            var stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(_syntaxTree, stringToken);
        }

        private ExpressionSyntax ParseNameOrCallExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.OpenParenthesisToken)
                return ParseCallExpression();

            if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.NamespaceToken && Peek(3).Kind != SyntaxKind.AccessToken)
            {
                var namespc = NextToken();
                MatchToken(SyntaxKind.NamespaceToken);
                var call = (CallExpressionSyntax)ParseCallExpression();
                call.Namespace = namespc.Text;
                return call;
            }

            if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.OpenBracketToken)
            {
                return ParseArrayExpression();
            }

            return ParseNameExpression();
        }

        private ExpressionSyntax ParseCallExpression()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseArguments();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new CallExpressionSyntax(_syntaxTree, identifier, openParenthesisToken, arguments, closeParenthesisToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextArgument = true;
            while (parseNextArgument &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var expression = ParseAssignmentExpression();
                nodesAndSeparators.Add(expression);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextArgument = false;
                }
            }

            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ExpressionSyntax ParseNameExpression()
        {
            if (Peek(1).Kind == SyntaxKind.AccessToken || (Peek(1).Kind == SyntaxKind.NamespaceToken && Peek(3).Kind == SyntaxKind.AccessToken))
                return ParseObjectAccessExpression();

            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);

            //if (Current.Kind == SyntaxKind.AccessToken)
            //{
            //    MatchToken(SyntaxKind.AccessToken);
            //    var internalToken = ParseCallExpression();
            //    return new RemoteNameExpressionSyntax(_syntaxTree, identifierToken, (CallExpressionSyntax)internalToken);
            //}

            return new NameExpressionSyntax(_syntaxTree, identifierToken);
        }

        ExpressionSyntax ParseObjectAccessExpression()
        {
            SyntaxToken package = null;

            if (Peek(1).Kind == SyntaxKind.NamespaceToken)
            {
                package = MatchToken(SyntaxKind.IdentifierToken);
                MatchToken(SyntaxKind.NamespaceToken);
            }

            SyntaxToken identifierToken = null;
            ExpressionSyntax expression = null;

            if (Current.Kind == SyntaxKind.AccessKeyword)
            {
                MatchToken(SyntaxKind.AccessKeyword);
                expression = ParseExpression();
            }
            else
                identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            
            MatchToken(SyntaxKind.AccessToken);

            if (Peek(1).Kind == SyntaxKind.OpenParenthesisToken)
            {
                var call = ParseCallExpression();
                return new ObjectAccessExpression(_syntaxTree, identifierToken, ObjectAccessExpression.AccessType.Call, (CallExpressionSyntax)call, null, null, package, expression);
            }
            if (Peek(1).Kind == SyntaxKind.AssignToken)
            {
                var propIdentifier = NextToken();
                MatchToken(SyntaxKind.AssignToken);
                var value = ParseExpression();
                return new ObjectAccessExpression(_syntaxTree, identifierToken, ObjectAccessExpression.AccessType.Set, null, propIdentifier, value, package, expression);
            }
            else if (Current.Kind == SyntaxKind.IdentifierToken)
            {
                var propIdentifier = NextToken();
                return new ObjectAccessExpression(_syntaxTree, identifierToken, ObjectAccessExpression.AccessType.Get, null, propIdentifier, null, package, expression);
            }

            return null;
        }

        ExpressionSyntax ParseExpressionAccessExpression(ExpressionSyntax expression)
        {
            MatchToken(SyntaxKind.AccessToken);
            ExpressionSyntax exp = null;

            if (Peek(1).Kind == SyntaxKind.OpenParenthesisToken)
            {
                var call = ParseCallExpression();
                exp = new ObjectAccessExpression(_syntaxTree, null, ObjectAccessExpression.AccessType.Call, (CallExpressionSyntax)call, null, null, null, expression);
            }
            else if (Peek(1).Kind == SyntaxKind.AssignToken)
            {
                var propIdentifier = NextToken();
                MatchToken(SyntaxKind.AssignToken);
                var value = ParseExpression();
                exp = new ObjectAccessExpression(_syntaxTree, null, ObjectAccessExpression.AccessType.Set, null, propIdentifier, value, null, expression);
            }
            else if (Current.Kind == SyntaxKind.IdentifierToken)
            {
                var propIdentifier = NextToken();
                exp = new ObjectAccessExpression(_syntaxTree, null, ObjectAccessExpression.AccessType.Get, null, propIdentifier, null, null, expression);
            }

            if (Current.Kind == SyntaxKind.AccessToken)
                return ParseExpressionAccessExpression(exp);

            return exp;
        }

        ExpressionSyntax ParseTernaryExpression(ExpressionSyntax condition)
        {
            MatchToken(SyntaxKind.QuestionMarkToken);
            var left = ParseExpression();
            MatchToken(SyntaxKind.ColonToken);
            var right = ParseExpression();
            

            return new TernaryExpressionSyntax(_syntaxTree, condition, left, right);
        }
    }
}