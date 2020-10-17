namespace ReCT.CodeAnalysis.Symbols
{
    public sealed class TypeSymbol  : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");
        public static readonly TypeSymbol Any = new TypeSymbol("any");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol Byte = new TypeSymbol("byte");
        public static readonly TypeSymbol String = new TypeSymbol("string");
        public static readonly TypeSymbol Void = new TypeSymbol("void");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol Thread = new TypeSymbol("thread");

        public static readonly TypeSymbol TCPClient = new TypeSymbol("tcpclient");
        public static readonly TypeSymbol TCPListener = new TypeSymbol("tcplistener");
        public static readonly TypeSymbol TCPSocket = new TypeSymbol("tcpsocket");

        //arrays
        public static readonly TypeSymbol AnyArr = new TypeSymbol("anyArr");
        public static readonly TypeSymbol BoolArr = new TypeSymbol("boolArr");
        public static readonly TypeSymbol IntArr = new TypeSymbol("intArr");
        public static readonly TypeSymbol ByteArr = new TypeSymbol("byteArr");
        public static readonly TypeSymbol StringArr = new TypeSymbol("stringArr");
        public static readonly TypeSymbol FloatArr = new TypeSymbol("floatArr");
        public static readonly TypeSymbol ThreadArr = new TypeSymbol("threadArr");

        public static readonly TypeSymbol TCPClientArr = new TypeSymbol("tcpclientArr");
        public static readonly TypeSymbol TCPListenerArr = new TypeSymbol("tcplistenerArr");
        public static readonly TypeSymbol TCPSocketArr = new TypeSymbol("tcpsocketArr");

        private TypeSymbol(string name)
            : base(name)
        {
        }

        public override SymbolKind Kind => SymbolKind.Type;
    }
}