using System;
using System.IO;
using ReCT.CodeAnalysis.Syntax;
using ReCT.IO;

namespace ReCT.CodeAnalysis.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(Symbol symbol, TextWriter writer)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Function:
                    WriteFunctionTo((FunctionSymbol)symbol, writer);
                    break;
                case SymbolKind.Class:
                    WriteClassTo((ClassSymbol)symbol, writer);
                    break;
                case SymbolKind.GlobalVariable:
                    WriteGlobalVariableTo((GlobalVariableSymbol)symbol, writer);
                    break;
                case SymbolKind.LocalVariable:
                    WriteLocalVariableTo((LocalVariableSymbol)symbol, writer);
                    break;
                case SymbolKind.FunctionalVariable:
                    WriteFunctionalVariableTo((FunctionalVariableSymbol)symbol, writer);
                    break;
                case SymbolKind.Parameter:
                    WriteParameterTo((ParameterSymbol)symbol, writer);
                    break;
                case SymbolKind.Type:
                    WriteTypeTo((TypeSymbol)symbol, writer);
                    break;
                default:
                    throw new Exception($"Unexpected symbol: {symbol.Kind}");
            }
        }

        private static void WriteFunctionTo(FunctionSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.FunctionKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            for (int i = 0; i < symbol.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                symbol.Parameters[i].WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);

            if (symbol.Type != TypeSymbol.Void)
            {
                writer.WritePunctuation(SyntaxKind.TypeToken);
                writer.WriteSpace();
                symbol.Type.WriteTo(writer);
            }
        }

        private static void WriteClassTo(ClassSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.ClassKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteGlobalVariableTo(GlobalVariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsReadOnly ? SyntaxKind.SetKeyword : SyntaxKind.VarKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.TypeToken);
            writer.WriteSpace();
          symbol.Type.WriteTo(writer);
        }

        private static void WriteLocalVariableTo(LocalVariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsReadOnly ? SyntaxKind.SetKeyword : SyntaxKind.VarKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.TypeToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteFunctionalVariableTo(FunctionalVariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsOverride ? SyntaxKind.OverrideKeyword : SyntaxKind.VirtualKeyword);
            writer.WriteSpace();
            writer.WriteKeyword(SyntaxKind.SetKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.TypeToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteParameterTo(ParameterSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.TypeToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteTypeTo(TypeSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
        }
    }
}