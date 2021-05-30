using System.Collections.Generic;

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
        public static readonly TypeSymbol Action = new TypeSymbol("action");

        //arrays
        public static readonly TypeSymbol AnyArr = new TypeSymbol("anyArr", true);
        public static readonly TypeSymbol BoolArr = new TypeSymbol("boolArr", true);
        public static readonly TypeSymbol IntArr = new TypeSymbol("intArr", true);
        public static readonly TypeSymbol ByteArr = new TypeSymbol("byteArr", true);
        public static readonly TypeSymbol StringArr = new TypeSymbol("stringArr", true);
        public static readonly TypeSymbol FloatArr = new TypeSymbol("floatArr", true);
        public static readonly TypeSymbol ThreadArr = new TypeSymbol("threadArr", true);
        public static readonly TypeSymbol ActionArr = new TypeSymbol("actionArr", true);

        //special control typesymbol
        public static readonly TypeSymbol ArrBase = new TypeSymbol("arrbase");

        public static Dictionary<ClassSymbol, TypeSymbol> Class;
        public static Dictionary<ClassSymbol, TypeSymbol> ClassArrays;

        public TypeSymbol(string name, bool array = false)
            : base(name)
        {
            isArray = array;
        }

        public EnumSymbol enumSymbol;
        public bool isEnum;
        public bool isFunc;
        public bool isClass;
        public bool isClassArray;
        public bool isArray;
        public override SymbolKind Kind => SymbolKind.Type;
    }
}