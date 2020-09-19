using System;
using System.Text;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Text;

namespace ReCT.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private int _position;

        private int _start;
        private SyntaxKind _kind;
        private object _value;

        public Lexer(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        private char Peek(int offset)
        {
            var index = _position + offset;

            if (index >= _text.Length)
                return '\0';

            return _text[index];
        }

        public SyntaxToken Lex()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;

                case '<' when Lookahead == '-' && Peek(2) == '+':
                    _kind = SyntaxKind.EditVariableToken;
                    _value = SyntaxKind.PlusToken;
                    _position += 3;
                    break;

                case '<' when Lookahead == '-' && Peek(2) == '-':
                    _kind = SyntaxKind.EditVariableToken;
                    _value = SyntaxKind.MinusToken;
                    _position += 3;
                    break;

                case '<' when Lookahead == '-' && Peek(2) == '*':
                    _kind = SyntaxKind.EditVariableToken;
                    _value = SyntaxKind.StarToken;
                    _position += 3;
                    break;

                case '<' when Lookahead == '-' && Peek(2) == '/':
                    _kind = SyntaxKind.EditVariableToken;
                    _value = SyntaxKind.SlashToken;
                    _position += 3;
                    break;

                case '+' when Lookahead == '+':
                    _kind = SyntaxKind.SingleEditVariableToken;
                    _value = SyntaxKind.PlusToken;
                    _position += 2;
                    break;

                case '-' when Lookahead == '-':
                    _kind = SyntaxKind.SingleEditVariableToken;
                    _value = SyntaxKind.MinusToken;
                    _position += 2;
                    break;

                case '+':
                    _kind = SyntaxKind.PlusToken;
                    _position++;
                    break;
                case '-' when Lookahead != '>':
                    _kind = SyntaxKind.MinusToken;
                    _position++;
                    break;
                case '*':
                    _kind = SyntaxKind.StarToken;
                    _position++;
                    break;
                case '/' when Lookahead != '/':
                    _kind = SyntaxKind.SlashToken;
                    _position++;
                    break;
                case '(':
                    _kind = SyntaxKind.OpenParenthesisToken;
                    _position++;
                    break;
                case ')':
                    _kind = SyntaxKind.CloseParenthesisToken;
                    _position++;
                    break;
                case '{':
                    _kind = SyntaxKind.OpenBraceToken;
                    _position++;
                    break;
                case '}':
                    _kind = SyntaxKind.CloseBraceToken;
                    _position++;
                    break;
                case '[':
                    _kind = SyntaxKind.OpenBracketToken;
                    _position++;
                    break;
                case ']':
                    _kind = SyntaxKind.CloseBracketToken;
                    _position++;
                    break;
                case ',':
                    _kind = SyntaxKind.CommaToken;
                    _position++;
                    break;
                case '~':
                    _kind = SyntaxKind.TildeToken;
                    _position++;
                    break;
                case '^':
                    _kind = SyntaxKind.HatToken;
                    _position++;
                    break;
                case '&' when Lookahead != '&':
                    _kind = SyntaxKind.AmpersandToken;
                    _position++;
                    break;
                case '&' when Lookahead == '&':
                    _kind = SyntaxKind.AmpersandAmpersandToken;
                    _position+=2;
                    break;
                case '|' when Lookahead != '|':
                    _kind = SyntaxKind.PipeToken;
                    _position++;
                    break;
                case '|' when Lookahead == '|':
                    _kind = SyntaxKind.PipePipeToken;
                    _position+=2;
                    break;
                case '=':
                    _kind = SyntaxKind.EqualsToken;
                    _position++;
                    break;
                case '!' when Lookahead != '=':
                    _kind = SyntaxKind.NotToken;
                    _position++;
                    break;
                case '!' when Lookahead == '=':
                    _kind = SyntaxKind.NotEqualsToken;
                    _position+=2;
                    break;
                case '<' when Lookahead == '-':
                    _kind = SyntaxKind.AssignToken;
                    _position += 2;
                    break;
                case '<' when Lookahead == '=':
                    _kind = SyntaxKind.LessOrEqualsToken;
                    _position+=2;
                    break;
                case '<' when Lookahead != '=' && Lookahead != '-':
                    _kind = SyntaxKind.LessToken;
                    _position++;
                    break;
                case '>' when Lookahead == '>':
                    _kind = SyntaxKind.AccessToken;
                    _position += 2;
                    break;
                case '>' when Lookahead != '=':
                    _position++;
                    _kind = SyntaxKind.GreaterToken;
                    break;
                case '>' when Lookahead == '=':
                    _kind = SyntaxKind.GreaterOrEqualsToken;
                    _position+=2;
                    break;
                case '-' when Lookahead == '>':
                    _kind = SyntaxKind.TypeToken;
                    _position+=2;
                    break;
                case '"':
                    ReadString();
                    break;
                case '/' when Lookahead == '/':
                    ReadComment();
                    break;
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    ReadNumber();
                    break;
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                case ';':
                    ReadWhiteSpace();
                    break;
                default:
                    if (char.IsLetter(Current))
                    {
                        ReadIdentifierOrKeyword();
                    }
                    else if (char.IsWhiteSpace(Current))
                    {
                        ReadWhiteSpace();
                    }
                    else
                    {
                        var span = new TextSpan(_position, 1);
                        var location = new TextLocation(_text, span);
                        _diagnostics.ReportBadCharacter(location, Current);
                        _position++;
                    }
                    break;
            }

            var length = _position - _start;
            var text = SyntaxFacts.GetText(_kind);
            if (text == null)
                text = _text.ToString(_start, length);

            return new SyntaxToken(_syntaxTree, _kind, _start, text, _value);
        }

        private void ReadString()
        {
            // Skip the current quote
            _position++;

            var sb = new StringBuilder();
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        var span = new TextSpan(_start, 1);
                        var location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedString(location);
                        done = true;
                        break;
                    case '"':
                        if (Lookahead == '"')
                        {
                            sb.Append(Current);
                            _position += 2;
                        }
                        else
                        {
                            _position++;
                            done = true;
                        }
                        break;
                    default:
                        sb.Append(Current);
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.StringToken;
            _value = sb.ToString();
        }

        private void ReadWhiteSpace()
        {
            while (char.IsWhiteSpace(Current) || Current == ';')
                _position++;

            _kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadComment()
        {
            while (Current != '\n' && Current != '\0')
                _position++;

            _kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadNumber()
        {
            bool isFloat = false;

            while (char.IsDigit(Current) || Current == '.')
            {
                _position++;
                if (Current == '.')
                    isFloat = true;
            }

            var length = _position - _start;
            var text = _text.ToString(_start, length);

            object value = null;

            if(isFloat)
            {
                if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                {
                    var span = new TextSpan(_start, length);
                    var location = new TextLocation(_text, span);
                    _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Float);
                }
                value = v;
            }
            else
            {
                if (!int.TryParse(text, out var v))
                {
                    var span = new TextSpan(_start, length);
                    var location = new TextLocation(_text, span);
                    _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Int);
                }
                value = v;
            }

            _value = value;
            _kind = SyntaxKind.NumberToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current) || char.IsDigit(Current))
                _position++;

            var length = _position - _start;
            var text = _text.ToString(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(text);
        }
    }
}