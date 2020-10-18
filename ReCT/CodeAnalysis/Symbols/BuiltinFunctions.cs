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
        public static readonly FunctionSymbol GetCursorX = new FunctionSymbol("GetCursorX", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol GetCursorY = new FunctionSymbol("GetCursorY", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol GetSizeX = new FunctionSymbol("GetSizeX", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol GetSizeY = new FunctionSymbol("GetSizeY", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol SetSize = new FunctionSymbol("SetSize", ImmutableArray.Create(new ParameterSymbol("X", TypeSymbol.Int, 0), new ParameterSymbol("Y", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol SetCursorVisible = new FunctionSymbol("SetCursorVisible", ImmutableArray.Create(new ParameterSymbol("bool", TypeSymbol.Bool, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol GetCursorVisible = new FunctionSymbol("GetCursorVisible", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Bool);
        public static readonly FunctionSymbol ConsoleColorBG = new FunctionSymbol("SetConsoleBackground", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol ConsoleColorFG = new FunctionSymbol("SetConsoleForeground", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Int, 0)), TypeSymbol.Void);
        
        public static readonly FunctionSymbol Beep = new FunctionSymbol("Beep", ImmutableArray.Create(new ParameterSymbol("freq", TypeSymbol.Int, 0), new ParameterSymbol("dur", TypeSymbol.Int, 0)), TypeSymbol.Void);

        //Networking
        public static readonly FunctionSymbol ConnectTCPClient = new FunctionSymbol("ConnectTCPClient", ImmutableArray.Create(new ParameterSymbol("address", TypeSymbol.String, 0), new ParameterSymbol("port", TypeSymbol.Int, 0)), TypeSymbol.TCPClient);
        public static readonly FunctionSymbol ListenOnTCPPort = new FunctionSymbol("ListenOnTCPPort", ImmutableArray.Create(new ParameterSymbol("port", TypeSymbol.Int, 0)), TypeSymbol.TCPListener);

        //Math
        public static readonly FunctionSymbol Random = new FunctionSymbol("Random", ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)), TypeSymbol.Int);
        public static readonly FunctionSymbol Floor = new FunctionSymbol("Floor", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Float, 0)), TypeSymbol.Int);
        public static readonly FunctionSymbol Ceil = new FunctionSymbol("Ceil", ImmutableArray.Create(new ParameterSymbol("num", TypeSymbol.Float, 0)), TypeSymbol.Int);

        //IO
        public static readonly FunctionSymbol ReadFile = new FunctionSymbol("ReadFile", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.String);
        public static readonly FunctionSymbol WriteFile = new FunctionSymbol("WriteFile", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0), new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol FileExists = new FunctionSymbol("FileExists", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.Bool);
        public static readonly FunctionSymbol DirectoryExists = new FunctionSymbol("DirectoryExists", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.Bool);
        public static readonly FunctionSymbol DeleteFile = new FunctionSymbol("DeleteFile", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol DeleteDirectory = new FunctionSymbol("DeleteDirectory", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol CreateDirectory = new FunctionSymbol("CreateDirectory", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol GetFilesInDir = new FunctionSymbol("GetFilesInDirectory", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.StringArr);
        public static readonly FunctionSymbol GetDirectoriesInDir = new FunctionSymbol("GetDirsInDirectory", ImmutableArray.Create(new ParameterSymbol("path", TypeSymbol.String, 0)), TypeSymbol.StringArr);

        //Other stuff
        public static readonly FunctionSymbol Version = new FunctionSymbol("Version", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol Sleep = new FunctionSymbol("Sleep", ImmutableArray.Create(new ParameterSymbol("int", TypeSymbol.Int, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol Char = new FunctionSymbol("Char", ImmutableArray.Create(new ParameterSymbol("int", TypeSymbol.Int, 0)), TypeSymbol.String);

        //String funcs
        public static readonly FunctionSymbol GetLength = new FunctionSymbol("GetLength", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);
        public static readonly FunctionSymbol Substring = new FunctionSymbol("Substring", ImmutableArray.Create(new ParameterSymbol("index", TypeSymbol.Int, 0), new ParameterSymbol("length", TypeSymbol.Int, 0)), TypeSymbol.String);

        //Thread functions
        public static readonly FunctionSymbol StartThread = new FunctionSymbol("StartThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
        public static readonly FunctionSymbol KillThread = new FunctionSymbol("KillThread", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);

        //Arr functions
        public static readonly FunctionSymbol GetArrayLength = new FunctionSymbol("GetArrayLength", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Int);

        //TCP functions
        public static readonly FunctionSymbol OpenSocket = new FunctionSymbol("OpenSocket", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.TCPSocket);

        public static readonly FunctionSymbol WriteToSocket = new FunctionSymbol("WriteToSocket", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol ReadSocket = new FunctionSymbol("ReadSocket", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol CloseSocket = new FunctionSymbol("CloseSocket", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
        public static readonly FunctionSymbol IsSocketConnected = new FunctionSymbol("IsSocketConnected", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Bool);


        public static readonly FunctionSymbol WriteToClient = new FunctionSymbol("WriteToClient", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Void);
        public static readonly FunctionSymbol ReadClient = new FunctionSymbol("ReadClient", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol CloseClient = new FunctionSymbol("CloseClient", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
        public static readonly FunctionSymbol IsClientConnected = new FunctionSymbol("IsClientConnected", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Bool);

        //Cube functions
        public static readonly FunctionSymbol Die = new FunctionSymbol("Die", ImmutableArray.Create(new ParameterSymbol("exitCode", TypeSymbol.Int, 0)), TypeSymbol.Int);

        //borger
        public static readonly FunctionSymbol Borger = new FunctionSymbol("borger", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);

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