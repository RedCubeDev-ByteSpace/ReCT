using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace ReCT.CodeAnalysis.Symbols
{
    internal static class BuiltinFunctions
    {
        //Console
        public static readonly FunctionSymbol Print = new FunctionSymbol("Print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol Write = new FunctionSymbol("Write", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol Input = new FunctionSymbol("Input", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol InputKey = new FunctionSymbol("InputKey", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol InputAction = new FunctionSymbol("InputAction", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol Clear = new FunctionSymbol("Clear", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
        public static readonly FunctionSymbol SetCursor = new FunctionSymbol("SetCursor", ImmutableArray.Create(new ParameterSymbol("xCoord", TypeSymbol.Int, 0), new ParameterSymbol("yCoord", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol GetSizeX = new FunctionSymbol("GetSizeX", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol GetSizeY = new FunctionSymbol("GetSizeY", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol SetSize = new FunctionSymbol("SetSize", ImmutableArray.Create(new ParameterSymbol("X", TypeSymbol.Int, 0), new ParameterSymbol("Y", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol SetCursorVisible = new FunctionSymbol("SetCursorVisible", ImmutableArray.Create(new ParameterSymbol("bool", TypeSymbol.Bool, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol GetCursorVisible = new FunctionSymbol("GetCursorVisible", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Bool);
        public static readonly FunctionSymbol ConsoleColorBG = new FunctionSymbol("SetConsoleBackground", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol ConsoleColorFG = new FunctionSymbol("SetConsoleForeground", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Int, 0)), TypeSymbol.Void);


        //Math
        public static readonly FunctionSymbol Random = new FunctionSymbol("Random", ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)), TypeSymbol.Int);

        //Other stuff
        public static readonly FunctionSymbol Version = new FunctionSymbol("Version", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol Sleep = new FunctionSymbol("Sleep", ImmutableArray.Create(new ParameterSymbol("int", TypeSymbol.Int, 0)), TypeSymbol.Void);

        //String funcs
        public static readonly FunctionSymbol GetLength = new FunctionSymbol("GetLength", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol Substring = new FunctionSymbol("Substring", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0), new ParameterSymbol("length", TypeSymbol.Int, 0)), TypeSymbol.String);

        //Thread functions
        public static readonly FunctionSymbol StartThread = new FunctionSymbol("StartThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
        public static readonly FunctionSymbol KillThread = new FunctionSymbol("KillThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);

        //Arr functions
        public static readonly FunctionSymbol GetArrayLength = new FunctionSymbol("GetArrayLength", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);

        //Cube functions
        public static readonly FunctionSymbol Die = new FunctionSymbol("Die", ImmutableArray.Create(new ParameterSymbol("exitCode", TypeSymbol.Int, 0)), TypeSymbol.Int);

        internal static IEnumerable<FunctionSymbol> GetAll()
            => typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                       .Where(f => f.FieldType == typeof(FunctionSymbol))
                                       .Select(f => (FunctionSymbol)f.GetValue(null));
        internal static IEnumerable<string> GetAllNames()
        {
            foreach(FunctionSymbol fs in GetAll())
            {
                yield return fs.Name;
            }
        }
        internal static FunctionSymbol getFuncFromName(string name)
        {
            List<FunctionSymbol> fns = new List<FunctionSymbol>(GetAll());
            for (int i = 0; i < fns.Count; i++)
            {
                if (fns[i].Name == name)
                    return fns[i];
            }
            return null;
        }
    }
}