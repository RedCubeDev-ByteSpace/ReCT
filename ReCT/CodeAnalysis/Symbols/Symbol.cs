using System.IO;
using ReCT.CodeAnalysis.Text;

namespace ReCT.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        private protected Symbol(string name, Text.TextLocation loc)
        {
            Name = name;
            Location = loc;
        }

        public abstract SymbolKind Kind { get; }
        public string Name { get; }
        public TextLocation Location { get; }

        public void WriteTo(TextWriter writer)
        {
            SymbolPrinter.WriteTo(this, writer);
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}