using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace ReCT.CodeAnalysis.Symbols
{
    internal static class BuiltinFunctions
    {
        

        //TypeFunctions

        //String functions
        public static readonly TypeFunctionSymbol GetLength = new TypeFunctionSymbol("GetLength", ImmutableArray<ParameterSymbol>.Empty,  TypeSymbol.String, TypeSymbol.Int);
        public static readonly TypeFunctionSymbol Split = new TypeFunctionSymbol("Split", ImmutableArray.Create(new ParameterSymbol("seperator", TypeSymbol.String, 0)), TypeSymbol.String, TypeSymbol.StringArr);
        public static readonly TypeFunctionSymbol Replace = new TypeFunctionSymbol("Replace", ImmutableArray.Create(new ParameterSymbol("original", TypeSymbol.String, 0), new ParameterSymbol("replacement", TypeSymbol.String, 1)), TypeSymbol.String, TypeSymbol.String);
        public static readonly TypeFunctionSymbol Substring = new TypeFunctionSymbol("Substring", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0), new ParameterSymbol("length", TypeSymbol.Int, 0)), TypeSymbol.String, TypeSymbol.String);
        public static readonly TypeFunctionSymbol At = new TypeFunctionSymbol("At", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0)), TypeSymbol.String, TypeSymbol.String);
        public static readonly TypeFunctionSymbol StartsWith = new TypeFunctionSymbol("StartsWith", ImmutableArray.Create(new ParameterSymbol("s", TypeSymbol.String, 0)), TypeSymbol.String, TypeSymbol.Bool);
        public static readonly TypeFunctionSymbol EndsWith = new TypeFunctionSymbol("EndsWith", ImmutableArray.Create(new ParameterSymbol("s", TypeSymbol.String, 0)), TypeSymbol.String, TypeSymbol.Bool);
        
        //Thread functions
        public static readonly TypeFunctionSymbol StartThread = new TypeFunctionSymbol("StartThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Thread, TypeSymbol.Void);
        public static readonly TypeFunctionSymbol KillThread = new TypeFunctionSymbol("KillThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Thread, TypeSymbol.Void);

        //Byte functions
        public static readonly TypeFunctionSymbol GetBit = new TypeFunctionSymbol("GetBit", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0)), TypeSymbol.Byte, TypeSymbol.Int);
        public static readonly TypeFunctionSymbol SetBit = new TypeFunctionSymbol("SetBit", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0), new ParameterSymbol("value", TypeSymbol.Int, 0)), TypeSymbol.Byte, TypeSymbol.Void);

        //Arr functions
        public static readonly TypeFunctionSymbol GetLengthArr = new TypeFunctionSymbol("GetLength", ImmutableArray<ParameterSymbol>.Empty,  TypeSymbol.AnyArr, TypeSymbol.Int);
        public static readonly TypeFunctionSymbol Push = new TypeFunctionSymbol("Push", ImmutableArray.Create(new ParameterSymbol("object", TypeSymbol.Any, 0)), TypeSymbol.AnyArr, TypeSymbol.Void);
        public static readonly TypeFunctionSymbol Pop = new TypeFunctionSymbol("Pop", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.AnyArr, TypeSymbol.ArrBase);
        public static readonly TypeFunctionSymbol AtArr = new TypeFunctionSymbol("At", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0)), TypeSymbol.AnyArr, TypeSymbol.ArrBase);
        
        //Action functions
        public static readonly TypeFunctionSymbol Run = new TypeFunctionSymbol("Run", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Action, TypeSymbol.Void);
        
        //Builtin functions
        public static readonly FunctionSymbol Die = new FunctionSymbol("die", ImmutableArray.Create(new ParameterSymbol("exitCode", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol Version = new FunctionSymbol("Version", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        //borger
        public static readonly FunctionSymbol Borger = new FunctionSymbol("borger", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);

        internal static IEnumerable<FunctionSymbol> GetAll()
            => typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                       .Where(f => f.FieldType == typeof(FunctionSymbol))
                                       .Select(f => (FunctionSymbol)f.GetValue(null));

        
        internal static IEnumerable<TypeFunctionSymbol> GetAllTypeFunctions()
            => typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                        .Where(f => f.FieldType == typeof(TypeFunctionSymbol))
                                        .Select(f => (TypeFunctionSymbol)f.GetValue(null));
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
