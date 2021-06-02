using System.Collections.Immutable;
using System.IO;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public BoundBlockStatement(ImmutableArray<BoundStatement> statements)
        {
            Statements = statements;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
        public ImmutableArray<BoundStatement> Statements { get; }

        public string StmtContent()
        {
            var writer = new StringWriter();
            BoundNodePrinter.WriteTo(this, writer);
            return writer.ToString();
        }
    }
}
