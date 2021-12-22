using System.Collections.Immutable;
using System.IO;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public BoundBlockStatement(ImmutableArray<BoundStatement> statements, BoundScope scope = null)
        {
            Statements = statements;
			Scope = scope;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
        public ImmutableArray<BoundStatement> Statements { get; }
        public BoundScope Scope { get; }

        public string StmtContent()
        {
            var writer = new StringWriter();
            BoundNodePrinter.WriteTo(this, writer);
            return writer.ToString();
        }
    }
}
