using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ReCT.CodeAnalysis.Binding;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReCT.CodeAnalysis.Emit
{
    internal sealed class Emitter
    {
        private DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes;
        private readonly TypeReference _consoleKeyInfoRef;
        private readonly TypeReference _charRef;
        private readonly TypeReference _doubleRef;
        private readonly TypeReference _StreamWriterRef;
        private readonly TypeReference _StreamReaderRef;
        private readonly MethodReference _objectConstructor;
        private readonly MethodReference _objectEqualsReference;
        private readonly MethodReference _consoleReadLineReference;
        private readonly MethodReference _consoleReadKeyReference;
        private readonly MethodReference _consoleGetKeyReference;
        private readonly MethodReference _consoleKeyInfoGetKeyChar;
        private readonly MethodReference _getVisableCursorRef;
        private readonly MethodReference _setVisableCursorRef;
        private readonly MethodReference _getCursorXReference;
        private readonly MethodReference _getCursorYReference;
        private readonly MethodReference _setConsoleFG;
        private readonly MethodReference _setConsoleBG;
        private readonly MethodReference _charToString;
        private readonly MethodReference _consoleWriteLineReference;
        private readonly MethodReference _consoleWriteReference;
        private readonly MethodReference _consoleClearReference;
        private readonly MethodReference _consoleSetCoursorReference;
        private readonly MethodReference _consoleGetHeightReference;
        private readonly MethodReference _consoleGetWidthReference;
        private readonly MethodReference _consoleSetSizeReference;
        private readonly MethodReference _consoleBeepReference;
        private readonly MethodReference _threadSlooopeReference;
        private readonly MethodReference _stringConcatReference;
        private readonly MethodReference _convertToBooleanReference;
        private readonly MethodReference _convertToInt32Reference;
        private readonly MethodReference _convertToUInt8Reference;
        private readonly MethodReference _convertToSingleReference;
        private readonly MethodReference _convertToStringReference;
        private readonly MethodReference _convertToDoubleReference;
        private readonly TypeReference _randomReference;
        private readonly MethodReference _randomCtorReference;
        private readonly MethodReference _randomNextReference;
        private readonly MethodReference _mathFloorReference;
        private readonly MethodReference _mathCeilReference;
        private readonly MethodReference _threadStartObjectReference;
        private readonly MethodReference _threadObjectReference;
        private readonly MethodReference _IOReadAllTextReference;
        private readonly MethodReference _IOWriteAllTextReference;
        private readonly MethodReference _IOFileExistsReference;
        private readonly MethodReference _IODirExistsReference;
        private readonly MethodReference _IOFileDeleteReference;
        private readonly MethodReference _IODirDeleteReference;
        private readonly MethodReference _IODirCreateReference;
        private readonly MethodReference _IOGetFilesInDirReference;
        private readonly MethodReference _IOGetDirsInDirReference;
        private readonly MethodReference _TCPClientCtorReference;
        private readonly MethodReference _TCPListenerCtorReference;
        private readonly MethodReference _TCPListenerStartReference;
        private readonly MethodReference _TCPAcceptSocketReference;
        private readonly MethodReference _TCPClientGetStream;
        private readonly MethodReference _TCPClientClose;
        private readonly MethodReference _TCPClientConnected;
        private readonly MethodReference _TCPNetworkStreamCtor;
        private readonly MethodReference _TCPSocketClose;
        private readonly MethodReference _TCPSocketConnected;
        private readonly MethodReference _IOStreamReaderCtor;
        private readonly MethodReference _IOReadLine;
        private readonly MethodReference _IOStreamWriterCtor;
        private readonly MethodReference _IOWriteLine;
        private readonly MethodReference _IOFlush;
        private readonly MethodReference _envDie;
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> _methods = new Dictionary<FunctionSymbol, MethodDefinition>();
        private readonly Dictionary<ClassSymbol, Dictionary<FunctionSymbol, MethodDefinition>> _classMethods = new Dictionary<ClassSymbol, Dictionary<FunctionSymbol, MethodDefinition>>();
        private readonly Dictionary<ClassSymbol, MethodReference[]> _packageClassMethods = new Dictionary<ClassSymbol, MethodReference[]>();
        private readonly Dictionary<ClassSymbol, FieldReference[]> _packageClassFields = new Dictionary<ClassSymbol, FieldReference[]>();
        private readonly Dictionary<ClassSymbol, TypeDefinition> _classes = new Dictionary<ClassSymbol, TypeDefinition>();
        private readonly Dictionary<string, Package.Package> _packages = new Dictionary<string, Package.Package>();
        private readonly Dictionary<string, MethodDefinition> str_methods = new Dictionary<string, MethodDefinition>();
        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals = new Dictionary<VariableSymbol, VariableDefinition>();
        private readonly Dictionary<VariableSymbol, FieldDefinition> _globals = new Dictionary<VariableSymbol, FieldDefinition>();
        private readonly Dictionary<TypeDefinition, Dictionary<VariableSymbol, FieldDefinition>> _classGlobals = new Dictionary<TypeDefinition, Dictionary<VariableSymbol, FieldDefinition>>();
        private readonly Dictionary<BoundLabel, int> _labels = new Dictionary<BoundLabel, int>();
        private readonly List<(int InstructionIndex, BoundLabel Target)> _fixups = new List<(int InstructionIndex, BoundLabel Target)>();
        
        private TypeDefinition inType = null;
        private ClassSymbol inClass = null;

        private TypeDefinition _typeDefinition;
        private FieldDefinition _randomFieldDefinition;
        private static List<AssemblyDefinition> s_assemblies;
        private static AssemblyDefinition s_assemblyDefinition;

        private Emitter(string moduleName, string[] references)
        {
            var assemblies = new List<AssemblyDefinition>();

            foreach (var reference in references)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(reference);
                    assemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    _diagnostics.ReportInvalidReference(reference);
                }
            }

            s_assemblies = assemblies;
            
            var builtInTypes = new List<(TypeSymbol type, string MetadataName)>()
            {
                (TypeSymbol.Any, "System.Object"),
                (TypeSymbol.Bool, "System.Boolean"),
                (TypeSymbol.Int, "System.Int32"),
                (TypeSymbol.Byte, "System.Byte"),
                (TypeSymbol.String, "System.String"),
                (TypeSymbol.Void, "System.Void"),
                (TypeSymbol.Float, "System.Single"),
                (TypeSymbol.Thread, "System.Threading.Thread"),
                (TypeSymbol.TCPClient, "System.Net.Sockets.TcpClient"),
                (TypeSymbol.TCPListener, "System.Net.Sockets.TcpListener"),
                (TypeSymbol.TCPSocket, "System.Net.Sockets.Socket"),
            };

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);
            _knownTypes = new Dictionary<TypeSymbol, TypeReference>();

            s_assemblyDefinition = _assemblyDefinition;

            _consoleKeyInfoRef = _assemblyDefinition.MainModule.ImportReference(assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).Where(t => t.FullName == "System.ConsoleKeyInfo").ToArray()[0]);
            _charRef = _assemblyDefinition.MainModule.ImportReference(assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).Where(t => t.FullName == "System.Char").ToArray()[0]);
            _doubleRef = _assemblyDefinition.MainModule.ImportReference(assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).Where(t => t.FullName == "System.Double").ToArray()[0]);
            _StreamWriterRef = _assemblyDefinition.MainModule.ImportReference(assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).Where(t => t.FullName == "System.IO.StreamWriter").ToArray()[0]);
            _StreamReaderRef = _assemblyDefinition.MainModule.ImportReference(assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).Where(t => t.FullName == "System.IO.StreamReader").ToArray()[0]);


            foreach (var (typeSymbol, metadataName) in builtInTypes)
            {
                var typeReference = ResolveType(typeSymbol.Name, metadataName);
                _knownTypes.Add(typeSymbol, typeReference);
            }


            TypeReference ResolveType(string rectName, string metadataName)
            {
                var foundTypes = assemblies.SelectMany(a => a.Modules)
                                           .SelectMany(m => m.Types)
                                           .Where(t => t.FullName == metadataName)
                                           .ToArray();
                if (foundTypes.Length == 1)
                {
                    var typeReference = _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                    return typeReference;
                }
                else if (foundTypes.Length == 0)
                {
                    _diagnostics.ReportRequiredTypeNotFound(rectName, metadataName);
                }
                else
                {
                    _diagnostics.ReportRequiredTypeAmbiguous(rectName, metadataName, foundTypes);
                }

                return null;
            }

            MethodReference ResolveMethod(string typeName, string methodName, string[] parameterTypeNames)
            {
                var foundTypes = assemblies.SelectMany(a => a.Modules)
                                           .SelectMany(m => m.Types)
                                           .Where(t => t.FullName == typeName)
                                           .ToArray();
                if (foundTypes.Length == 1)
                {
                    var foundType = foundTypes[0];
                    var methods = foundType.Methods.Where(m => m.Name == methodName);

                    foreach (var method in methods)
                    {
                        if (method.Parameters.Count != parameterTypeNames.Length)
                            continue;

                        var allParametersMatch = true;

                        for (var i = 0; i < parameterTypeNames.Length; i++)
                        {
                            if (method.Parameters[i].ParameterType.FullName != parameterTypeNames[i])
                            {
                                allParametersMatch = false;
                                break;
                            }
                        }

                        if (!allParametersMatch)
                            continue;

                        return _assemblyDefinition.MainModule.ImportReference(method);
                    }

                    _diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                    return null;
                }
                else if (foundTypes.Length == 0)
                {
                    _diagnostics.ReportRequiredTypeNotFound(null, typeName);
                }
                else
                {
                    _diagnostics.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
                }

                return null;
            }

            //register array types
            _knownTypes.Add(TypeSymbol.AnyArr, _knownTypes[TypeSymbol.Any].MakeArrayType());
            _knownTypes.Add(TypeSymbol.IntArr, _knownTypes[TypeSymbol.Int].MakeArrayType());
            _knownTypes.Add(TypeSymbol.ByteArr, _knownTypes[TypeSymbol.Byte].MakeArrayType());
            _knownTypes.Add(TypeSymbol.BoolArr, _knownTypes[TypeSymbol.Bool].MakeArrayType());
            _knownTypes.Add(TypeSymbol.StringArr, _knownTypes[TypeSymbol.String].MakeArrayType());
            _knownTypes.Add(TypeSymbol.FloatArr, _knownTypes[TypeSymbol.Float].MakeArrayType());
            _knownTypes.Add(TypeSymbol.ThreadArr, _knownTypes[TypeSymbol.Thread].MakeArrayType());
            _knownTypes.Add(TypeSymbol.TCPClientArr, _knownTypes[TypeSymbol.TCPClient].MakeArrayType());
            _knownTypes.Add(TypeSymbol.TCPListenerArr, _knownTypes[TypeSymbol.TCPListener].MakeArrayType());
            _knownTypes.Add(TypeSymbol.TCPSocketArr, _knownTypes[TypeSymbol.TCPSocket].MakeArrayType());

            _objectConstructor = ResolveMethod("System.Object", ".ctor", Array.Empty<string>());

            _objectEqualsReference = ResolveMethod("System.Object", "Equals", new[] { "System.Object", "System.Object" });

            _consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            _consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] { "System.String" });
            _consoleWriteReference = ResolveMethod("System.Console", "Write", new[] { "System.String" });
            _consoleClearReference = ResolveMethod("System.Console", "Clear", Array.Empty<string>());
            _consoleSetCoursorReference = ResolveMethod("System.Console", "SetCursorPosition", new[] { "System.Int32", "System.Int32" });

            //ReadKey
            _consoleReadKeyReference = ResolveMethod("System.Console", "ReadKey", Array.Empty<string>());
            _consoleGetKeyReference = ResolveMethod("System.Console", "ReadKey", new[] { "System.Boolean" });
            _consoleKeyInfoGetKeyChar = ResolveMethod("System.ConsoleKeyInfo", "get_KeyChar", Array.Empty<string>());

            //cursor visibility
            _getVisableCursorRef = ResolveMethod("System.Console", "get_CursorVisible", Array.Empty<string>());
            _setVisableCursorRef = ResolveMethod("System.Console", "set_CursorVisible", new[] { "System.Boolean" });

            //cursor getPos
            _getCursorXReference = ResolveMethod("System.Console", "get_CursorLeft", Array.Empty<string>());
            _getCursorYReference = ResolveMethod("System.Console", "get_CursorTop", Array.Empty<string>());

            //fg and bg color
            _setConsoleFG = ResolveMethod("System.Console", "set_ForegroundColor", new[] { "System.ConsoleColor" });
            _setConsoleBG = ResolveMethod("System.Console", "set_BackgroundColor", new[] { "System.ConsoleColor" });

            _charToString = ResolveMethod("System.Char", "ToString", Array.Empty<string>());

            //console functions
            _consoleGetHeightReference = ResolveMethod("System.Console", "get_WindowHeight", Array.Empty<string>());
            _consoleGetWidthReference = ResolveMethod("System.Console", "get_WindowWidth", Array.Empty<string>());

            _consoleSetSizeReference = ResolveMethod("System.Console", "SetWindowSize", new[] { "System.Int32", "System.Int32" });

            _consoleBeepReference = ResolveMethod("System.Console", "Beep", new[] { "System.Int32", "System.Int32" });


            _threadSlooopeReference = ResolveMethod("System.Threading.Thread", "Sleep", new[] { "System.Int32" });

            _stringConcatReference = ResolveMethod("System.String", "Concat", new[] { "System.String", "System.String" });
            _convertToBooleanReference = ResolveMethod("System.Convert", "ToBoolean", new[] { "System.Object" });
            _convertToInt32Reference = ResolveMethod("System.Convert", "ToInt32", new[] { "System.Object" });
            _convertToUInt8Reference = ResolveMethod("System.Convert", "ToByte", new[] { "System.Object" });
            _convertToSingleReference = ResolveMethod("System.Convert", "ToSingle", new[] { "System.Object" });
            _convertToStringReference = ResolveMethod("System.Convert", "ToString", new[] { "System.Object" });
            _convertToDoubleReference = ResolveMethod("System.Convert", "ToDouble", new[] { "System.Object" });
            _randomReference = ResolveType(null, "System.Random");
            _randomCtorReference = ResolveMethod("System.Random", ".ctor", Array.Empty<string>());
            _randomNextReference = ResolveMethod("System.Random", "Next", new[] { "System.Int32" });

            //Meth
            _mathFloorReference = ResolveMethod("System.Math", "Floor", new[] { "System.Double" });
            _mathCeilReference = ResolveMethod("System.Math", "Ceiling", new[] { "System.Double" });

            //threading
            _threadStartObjectReference = ResolveMethod("System.Threading.ThreadStart", ".ctor", new[] { "System.Object", "System.IntPtr" });
            _threadObjectReference = ResolveMethod("System.Threading.Thread", ".ctor", new[] { "System.Threading.ThreadStart" });

            //IO
            _IOReadAllTextReference = ResolveMethod("System.IO.File", "ReadAllText", new[] { "System.String" });
            _IOWriteAllTextReference = ResolveMethod("System.IO.File", "WriteAllText", new[] { "System.String", "System.String" });

            _IOFileExistsReference = ResolveMethod("System.IO.File", "Exists", new[] { "System.String" });
            _IODirExistsReference = ResolveMethod("System.IO.Directory", "Exists", new[] { "System.String" });

            _IOFileDeleteReference = ResolveMethod("System.IO.File", "Delete", new[] { "System.String" });
            _IODirDeleteReference = ResolveMethod("System.IO.Directory", "Delete", new[] { "System.String" });

            _IODirCreateReference = ResolveMethod("System.IO.Directory", "CreateDirectory", new[] { "System.String" });

            _IOGetFilesInDirReference = ResolveMethod("System.IO.Directory", "GetFiles", new[] { "System.String" });
            _IOGetDirsInDirReference = ResolveMethod("System.IO.Directory", "GetDirectories", new[] { "System.String" });

            //TCP Networking
            _TCPClientCtorReference = ResolveMethod("System.Net.Sockets.TcpClient", ".ctor", new[] { "System.String", "System.Int32" });
            _TCPListenerCtorReference = ResolveMethod("System.Net.Sockets.TcpListener", ".ctor", new[] { "System.Int32" });
            _TCPListenerStartReference = ResolveMethod("System.Net.Sockets.TcpListener", "Start", Array.Empty<string>());

            _TCPAcceptSocketReference = ResolveMethod("System.Net.Sockets.TcpListener", "AcceptSocket", Array.Empty<string>());

            _TCPClientGetStream = ResolveMethod("System.Net.Sockets.TcpClient", "GetStream", Array.Empty<string>());
            _TCPClientClose = ResolveMethod("System.Net.Sockets.TcpClient", "Close", Array.Empty<string>());
            _TCPClientConnected = ResolveMethod("System.Net.Sockets.TcpClient", "get_Connected", Array.Empty<string>());
            _TCPNetworkStreamCtor = ResolveMethod("System.Net.Sockets.NetworkStream", ".ctor", new[] { "System.Net.Sockets.Socket" });

            _TCPSocketClose = ResolveMethod("System.Net.Sockets.Socket", "Close", Array.Empty<string>());
            _TCPSocketConnected = ResolveMethod("System.Net.Sockets.Socket", "get_Connected", Array.Empty<string>());

            _IOStreamReaderCtor = ResolveMethod("System.IO.StreamReader", ".ctor", new[] { "System.IO.Stream" });
            _IOReadLine = ResolveMethod("System.IO.TextReader", "ReadLine", Array.Empty<string>());
            _IOStreamWriterCtor = ResolveMethod("System.IO.StreamWriter", ".ctor", new[] { "System.IO.Stream" });
            _IOWriteLine = ResolveMethod("System.IO.TextWriter", "WriteLine", new[] { "System.String" });
            _IOFlush = ResolveMethod("System.IO.TextWriter", "Flush", Array.Empty<string>());

            //die
            _envDie = ResolveMethod("System.Environment", "Exit", new[] { "System.Int32" });
        }

        public TypeReference ResolveTypePublic(string rectName, string metadataName)
        {
            var foundTypes = s_assemblies.SelectMany(a => a.Modules)
                                       .SelectMany(m => m.Types)
                                       .Where(t => t.FullName == metadataName)
                                       .ToArray();
            if (foundTypes.Length == 1)
            {
                var typeReference = _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                return typeReference;
            }
            else if (foundTypes.Length == 0)
            {
                _diagnostics.ReportRequiredTypeNotFound(rectName, metadataName);
            }
            else
            {
                _diagnostics.ReportRequiredTypeAmbiguous(rectName, metadataName, foundTypes);
            }

            return null;
        }

        public MethodReference ResolveMethodPublic(string typeName, string methodName, string[] parameterTypeNames)
        {
            var foundTypes = s_assemblies.SelectMany(a => a.Modules)
                                       .SelectMany(m => m.Types)
                                       .Where(t => t.FullName == typeName)
                                       .ToArray();
            if (foundTypes.Length == 1)
            {
                var foundType = foundTypes[0];
                var methods = foundType.Methods.Where(m => m.Name == methodName);

                foreach (var method in methods)
                {
                    if (method.Parameters.Count != parameterTypeNames.Length)
                        continue;

                    var allParametersMatch = true;

                    for (var i = 0; i < parameterTypeNames.Length; i++)
                    {
                        if (method.Parameters[i].ParameterType.FullName != parameterTypeNames[i])
                        {
                            allParametersMatch = false;
                            break;
                        }
                    }

                    if (!allParametersMatch)
                        continue;

                    return s_assemblyDefinition.MainModule.ImportReference(method);
                }

                _diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                return null;
            }
            else if (foundTypes.Length == 0)
            {
                _diagnostics.ReportRequiredTypeNotFound(null, typeName);
            }
            else
            {
                _diagnostics.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
            }

            return null;
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, string moduleName, string[] references, string outputPath)
        {
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            var emitter = new Emitter(moduleName, references);
            return emitter.Emit(program, outputPath);
        }

        public ImmutableArray<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            if (_diagnostics.Any())
                return _diagnostics.ToImmutableArray();

            //packages
            foreach (Package.Package p in program.Packages)
            {
                var assemblyDef = AssemblyDefinition.ReadAssembly(p.fullName);
                s_assemblies.Add(assemblyDef);

                foreach (ClassSymbol c in p.scope.GetDeclaredClasses())
                {
                    var typeRef = s_assemblies.SelectMany(a => a.Modules).SelectMany(m => m.Types).SelectMany(t => t.NestedTypes).FirstOrDefault(nt => nt.FullName == p.name + "." + p.name + "/" + c.Name);
                    var asmRef = s_assemblyDefinition.MainModule.ImportReference(typeRef);
                    _knownTypes.Add(TypeSymbol.Class[c], asmRef);

                    //if (!c.IsStatic)
                    //{
                    //var type = TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == c.Name + "Arr");
                    //_knownTypes.Add(type.Value, asmRef.MakeArrayType());
                    //}

                    List<MethodReference> mrefs = new List<MethodReference>();
                    List<FieldReference> frefs = new List<FieldReference>();

                    foreach (MethodDefinition m in typeRef.Methods)
                    {
                        mrefs.Add(s_assemblyDefinition.MainModule.ImportReference(m));
                    }
                    foreach (FieldDefinition f in typeRef.Fields)
                    {
                        frefs.Add(s_assemblyDefinition.MainModule.ImportReference(f));
                    }
                    _packageClassMethods.Add(c, mrefs.ToArray());
                    _packageClassFields.Add(c, frefs.ToArray());
                }

                _packages.Add(p.name, p);
            }

            var objectType = _knownTypes[TypeSymbol.Any];
            _typeDefinition = new TypeDefinition(program.Namespace, program.Type == "" ? "Program" : program.Type, TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public, objectType);
            _assemblyDefinition.MainModule.Types.Add(_typeDefinition);

            //classes
            var classDefinitions = new Dictionary<KeyValuePair<ClassSymbol, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>>, TypeDefinition>();

            //Register class names
            foreach (var _class in program.Classes)
            {
                var classDefinition = new TypeDefinition(program.Namespace, _class.Key.Name, (_class.Key.IsStatic ? (TypeAttributes.Abstract | TypeAttributes.Sealed) : 0) | TypeAttributes.Public, objectType);
                _assemblyDefinition.MainModule.Types.Add(classDefinition);

                _classes.Add(_class.Key, classDefinition);

                inType = classDefinition;
                inClass = _class.Key;
                _classGlobals.Add(classDefinition, new Dictionary<VariableSymbol, FieldDefinition>());
                _knownTypes.Add(TypeSymbol.Class[_class.Key], classDefinition);
                
                if(!_class.Key.IsStatic)
                    _knownTypes.Add(TypeSymbol.Class.FirstOrDefault(x => x.Key.Name == _class.Key.Name + "Arr").Value , classDefinition.MakeArrayType());
                
                _classMethods.Add(_class.Key, new Dictionary<FunctionSymbol, MethodDefinition>());

                classDefinitions.Add(_class, classDefinition);
            }

            //register function names
            foreach (var _classDef in classDefinitions)
            {
                var _class = _classDef.Key;
                var classDefinition = _classDef.Value;

                inType = classDefinition;
                inClass = _class.Key;

                foreach (var functionSB in _class.Value)
                {
                    var function = functionSB.Key;
                    var body = functionSB.Value;

                    //decleration
                    var functionType = _knownTypes[function.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == function.Type.Name) : function.Type];
                    var method = new MethodDefinition(function.Name, (_class.Key.IsStatic ? MethodAttributes.Static : 0) | (function.IsPublic ? MethodAttributes.Public : MethodAttributes.Private), functionType);

                    if (function.Name == "Constructor")
                    {
                        method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _knownTypes[TypeSymbol.Void]);
                    }

                    foreach (var parameter in function.Parameters)
                    {
                        var parameterType = _knownTypes[parameter.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == parameter.Type.Name) : parameter.Type];
                        var parameterAttributes = ParameterAttributes.None;
                        var parameterDefinition = new ParameterDefinition(parameter.Name, parameterAttributes, parameterType);
                        method.Parameters.Add(parameterDefinition);
                    }

                    classDefinition.Methods.Add(method);
                    _classMethods[_class.Key].Add(function, method);
                }
            }

            //register constructor bodies
            foreach (var _classDef in classDefinitions)
            {
                var _class = _classDef.Key;
                var classDefinition = _classDef.Value;

                inType = classDefinition;
                inClass = _class.Key;

                if (_class.Value.FirstOrDefault(x => x.Key.Name == "Constructor").Value != null)
                {
                    var pair = _class.Value.FirstOrDefault(x => x.Key.Name == "Constructor");
                    var function = pair.Key;
                    var body = pair.Value;
                    var method = _classMethods[_class.Key][function];

                    _class.Key.hasConstructor = true;

                     //body
                    _locals.Clear();
                    _labels.Clear();
                    _fixups.Clear();

                    var ilProcessor = method.Body.GetILProcessor();

                    ilProcessor.Emit(OpCodes.Ldarg_0);
                    ilProcessor.Emit(OpCodes.Call, _objectConstructor);
                    ilProcessor.Emit(OpCodes.Nop);

                    foreach (var statement in body.Statements)
                        EmitStatement(ilProcessor, statement);

                    foreach (var fixup in _fixups)
                    {
                        var targetLabel = fixup.Target;
                        var targetInstructionIndex = _labels[targetLabel];
                        var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                        var instructionToFixup = ilProcessor.Body.Instructions[fixup.InstructionIndex];
                        instructionToFixup.Operand = targetInstruction;
                    }

                    method.Body.OptimizeMacros();
                }
            }

            inClass = null;
            inType = null;

            //register main class functions
            foreach (var functionWithBody in program.Functions)
                EmitFunctionDeclaration(functionWithBody.Key);

            if (program.MainFunction != null)
                EmitFunctionBody(program.MainFunction, program.Functions.FirstOrDefault(x => x.Key == program.MainFunction).Value);

            foreach (var functionWithBody in program.Functions)
                if (functionWithBody.Key != program.MainFunction)
                    EmitFunctionBody(functionWithBody.Key, functionWithBody.Value);


            //register function bodies
            foreach (var _classDef in classDefinitions)
            {
                var _class = _classDef.Key;
                var classDefinition = _classDef.Value;

                var hasContructor = _class.Key.hasConstructor;

                inType = classDefinition;
                inClass = _class.Key;
                //Console.WriteLine(inClass.Name + (inClass.IsStatic ? " is STATIC" : " is DYNAMIC"));
                //Dictionary<FunctionSymbol, MethodDefinition> classMethods = new Dictionary<FunctionSymbol, MethodDefinition>();
                
                foreach (var functionSB in _class.Value)
                {
                    if (functionSB.Key.Name == "Constructor") continue;

                    var function = functionSB.Key;
                    var body = functionSB.Value;
                    var method = _classMethods[_class.Key][function];

                    //body
                    _locals.Clear();
                    _labels.Clear();
                    _fixups.Clear();

                    var ilProcessor = method.Body.GetILProcessor();

                    foreach (var statement in body.Statements)
                        EmitStatement(ilProcessor, statement);

                    foreach (var fixup in _fixups)
                    {
                        var targetLabel = fixup.Target;
                        var targetInstructionIndex = _labels[targetLabel];
                        var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                        var instructionToFixup = ilProcessor.Body.Instructions[fixup.InstructionIndex];
                        instructionToFixup.Operand = targetInstruction;
                    }

                    method.Body.OptimizeMacros();
                }

                //generate constructor if needed
                if (!_class.Key.IsStatic && !hasContructor)
                {
                    var method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _knownTypes[TypeSymbol.Void]);

                    classDefinition.Methods.Add(method);
                    _classMethods[_class.Key].Add(new FunctionSymbol("Constructor", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void), method);

                    //body
                    _locals.Clear();
                    _labels.Clear();
                    _fixups.Clear();

                    var ilProcessor = method.Body.GetILProcessor();

                    ilProcessor.Emit(OpCodes.Ldarg_0);
                    ilProcessor.Emit(OpCodes.Call, _objectConstructor);
                    ilProcessor.Emit(OpCodes.Nop);
                    ilProcessor.Emit(OpCodes.Ret);

                    method.Body.OptimizeMacros();
                }
            }
            
            inType = null;
            inClass = null;

            if (program.MainFunction != null)
                _assemblyDefinition.EntryPoint = _methods[program.MainFunction];

            _assemblyDefinition.Write(outputPath);

            return _diagnostics.ToImmutableArray();
        }

        private void EmitFunctionDeclaration(FunctionSymbol function)
        {
            var functionType = _knownTypes[function.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == function.Type.Name) : function.Type];
            var method = new MethodDefinition(function.Name, MethodAttributes.Static | (function.IsPublic ? MethodAttributes.Public : MethodAttributes.Private), functionType);

            foreach (var parameter in function.Parameters)
            {
                var parameterType = _knownTypes[parameter.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == parameter.Type.Name) : parameter.Type];
                var parameterAttributes = ParameterAttributes.None;
                var parameterDefinition = new ParameterDefinition(parameter.Name, parameterAttributes, parameterType);
                method.Parameters.Add(parameterDefinition);
            }

            _typeDefinition.Methods.Add(method);
            _methods.Add(function, method);
            str_methods.Add(function.Name, method);
        }

        private void EmitFunctionBody(FunctionSymbol function, BoundBlockStatement body)
        {
            var method = _methods[function];
            _locals.Clear();
            _labels.Clear();
            _fixups.Clear();

            var ilProcessor = method.Body.GetILProcessor();

            foreach (var statement in body.Statements)
                EmitStatement(ilProcessor, statement);

            foreach (var fixup in _fixups)
            {
                var targetLabel = fixup.Target;
                var targetInstructionIndex = _labels[targetLabel];
                var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                var instructionToFixup = ilProcessor.Body.Instructions[fixup.InstructionIndex];
                instructionToFixup.Operand = targetInstruction;
            }

            method.Body.OptimizeMacros();
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement node)
        {
            if (node == null)
                return;

            switch (node.Kind)
            {
                case BoundNodeKind.VariableDeclaration:
                    EmitVariableDeclaration(ilProcessor, (BoundVariableDeclaration)node);
                    break;
                case BoundNodeKind.LabelStatement:
                    EmitLabelStatement(ilProcessor, (BoundLabelStatement)node);
                    break;
                case BoundNodeKind.GotoStatement:
                    EmitGotoStatement(ilProcessor, (BoundGotoStatement)node);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    EmitConditionalGotoStatement(ilProcessor, (BoundConditionalGotoStatement)node);
                    break;
                case BoundNodeKind.ReturnStatement:
                    EmitReturnStatement(ilProcessor, (BoundReturnStatement)node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EmitExpressionStatement(ilProcessor, (BoundExpressionStatement)node);
                    break;
                case BoundNodeKind.TryCatchStatement:
                    EmitTryCatchStatement(ilProcessor, (BoundTryCatchStatement)node);
                    break;
                case BoundNodeKind.BlockStatement:
                    EmitBlockStatement(ilProcessor, (BoundBlockStatement)node);
                    break;
                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitBlockStatement(ILProcessor ilProcessor, BoundBlockStatement node)
        {
            foreach (BoundStatement s in node.Statements)
                EmitStatement(ilProcessor, s);
        }

        private void EmitTryCatchStatement(ILProcessor ilProcessor, BoundTryCatchStatement node)
        {
            var nooooooooop = ilProcessor.Create(OpCodes.Nop);
            ilProcessor.Append(nooooooooop);

            //foreach (var statement in node.NormalStatement.Statements)
            EmitStatement(ilProcessor, node.NormalStatement);

            var nop = ilProcessor.Create(OpCodes.Nop);
            var leave = ilProcessor.Create(OpCodes.Leave, nop);
            ilProcessor.Append(leave);

            var noOp = ilProcessor.Create(OpCodes.Nop);
            ilProcessor.Append(noOp);

            //foreach (var statement in node.ExceptionStatement.Statements)
            EmitStatement(ilProcessor, node.ExceptionStatement);

            var leaf = ilProcessor.Create(OpCodes.Leave, nop);
            ilProcessor.Append(leaf);

            ilProcessor.Append(nop);


            ExceptionHandler e = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = _knownTypes[TypeSymbol.Any],
                TryStart = nooooooooop,
                TryEnd = noOp,
                HandlerStart = noOp,
                HandlerEnd = nop
            };

            ilProcessor.Body.ExceptionHandlers.Add(e);
        }

        private void EmitVariableDeclaration(ILProcessor ilProcessor, BoundVariableDeclaration node)
        {
            var typeReference = _knownTypes[node.Variable.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == node.Variable.Type.Name) : node.Variable.Type];
            var variableDefinition = new VariableDefinition(typeReference);
            FieldDefinition field = null;

            if (node.Variable.IsGlobal)
            {
                field = EmitGlobalVar(ilProcessor, node);
                (inType == null ? _globals : _classGlobals[inType]).Add(node.Variable, field);
            }
            else
            {
                _locals.Add(node.Variable, variableDefinition);
                ilProcessor.Body.Variables.Add(variableDefinition);
            }

            if (inClass != null && !inClass.IsStatic && node.Variable.IsGlobal)
                ilProcessor.Emit(OpCodes.Ldarg_0);

            EmitExpression(ilProcessor, node.Initializer);

            if (inType != null)
            {
                if (!node.Variable.IsGlobal)
                {
                    ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
                    return;
                }

                if (inClass.IsStatic)
                    ilProcessor.Emit(OpCodes.Stsfld, field);
                else
                    ilProcessor.Emit(OpCodes.Stfld, field);

                return;
            }

            if (node.Variable.IsGlobal)
                ilProcessor.Emit(OpCodes.Stsfld, field);
            else
                ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private FieldDefinition EmitGlobalVar(ILProcessor ilProcessor, BoundVariableDeclaration node)
        {
            var _globalField = new FieldDefinition(
                "$" + node.Variable.Name, inType == null ? FieldAttributes.Static : (inClass.IsStatic ? FieldAttributes.Static : 0) | FieldAttributes.Public,
                _knownTypes[_knownTypes.Keys.FirstOrDefault(x => x.Name == node.Variable.Type.Name)]
            );
            (inType == null ? _typeDefinition : inType).Fields.Add(_globalField);
            return _globalField;
        }

        private void EmitLabelStatement(ILProcessor ilProcessor, BoundLabelStatement node)
        {
            _labels.Add(node.Label, ilProcessor.Body.Instructions.Count);
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement node)
        {
            _fixups.Add((ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement node)
        {
            EmitExpression(ilProcessor, node.Condition);

            var opCode = node.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
            _fixups.Add((ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(opCode, Instruction.Create(OpCodes.Nop));
        }

        private void EmitReturnStatement(ILProcessor ilProcessor, BoundReturnStatement node)
        {
            if (node.Expression != null)
                EmitExpression(ilProcessor, node.Expression);

            ilProcessor.Emit(OpCodes.Ret);
        }

        private void EmitExpressionStatement(ILProcessor ilProcessor, BoundExpressionStatement node)
        {
            EmitExpression(ilProcessor, node.Expression);

            if (node.Expression.Type != TypeSymbol.Void && !(node.Expression is BoundAssignmentExpression))
            {
                ilProcessor.Emit(OpCodes.Pop);
            }
        }

        private void EmitExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    EmitLiteralExpression(ilProcessor, (BoundLiteralExpression)node);
                    break;
                case BoundNodeKind.VariableExpression:
                    EmitVariableExpression(ilProcessor, (BoundVariableExpression)node);
                    break;
                case BoundNodeKind.ObjectAccessExpression:
                    EmitObjectAccessExpression(ilProcessor, (BoundObjectAccessExpression)node);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    EmitAssignmentExpression(ilProcessor, (BoundAssignmentExpression)node);
                    break;
                case BoundNodeKind.UnaryExpression:
                    EmitUnaryExpression(ilProcessor, (BoundUnaryExpression)node);
                    break;
                case BoundNodeKind.BinaryExpression:
                    EmitBinaryExpression(ilProcessor, (BoundBinaryExpression)node);
                    break;
                case BoundNodeKind.CallExpression:
                    EmitCallExpression(ilProcessor, (BoundCallExpression)node);
                    break;
                case BoundNodeKind.ConversionExpression:
                    EmitConversionExpression(ilProcessor, (BoundConversionExpression)node);
                    break;
                case BoundNodeKind.ThreadCreateExpression:
                    EmitThreadCreate(ilProcessor, (BoundThreadCreateExpression)node);
                    break;
                case BoundNodeKind.ArrayCreationExpression:
                    EmitArrayCreate(ilProcessor, (BoundArrayCreationExpression)node);
                    break;
                case BoundNodeKind.ObjectCreationExpression:
                    EmitObjectCreate(ilProcessor, (BoundObjectCreationExpression)node);
                    break;
                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitObjectCreate(ILProcessor ilProcessor, BoundObjectCreationExpression node)
        {
            foreach (var argument in node.Arguments)
                EmitExpression(ilProcessor, argument);

            MethodDefinition ctor = null;

            if (node.Package == null)
                ctor = _classMethods[node.Class].FirstOrDefault(x => x.Key.Name == "Constructor").Value;
            else
            {
                List<string> args = new List<string>();

                var constructor = node.Package.scope.GetDeclaredClasses().FirstOrDefault(x => x == node.Class).Scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == "Constructor");
                foreach (ParameterSymbol p in constructor.Parameters)
                    args.Add(_knownTypes[p.Type].FullName);

                //var ctorr = ResolveMethodPublic(node.Package.name + "." + node.Package.name + "/" + node.Class.Name, ".ctor", args.ToArray());
                var ctorr = _packageClassMethods[node.Class].FirstOrDefault(x => x.Name == ".ctor");
                ilProcessor.Emit(OpCodes.Newobj, ctorr);

                return;
            }


            ilProcessor.Emit(OpCodes.Newobj, ctor);
        }

        private void EmitArrayCreate(ILProcessor ilProcessor, BoundArrayCreationExpression node)
        {
            EmitExpression(ilProcessor, node.Length);
            ilProcessor.Emit(OpCodes.Newarr, _knownTypes[node.ArrayType.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == node.ArrayType.Name) : node.ArrayType]);
        }

        private void EmitThreadCreate(ILProcessor ilProcessor, BoundThreadCreateExpression node)
        {
            ilProcessor.Emit(OpCodes.Ldnull);
            ilProcessor.Emit(OpCodes.Ldftn, _methods[node.Function]);
            ilProcessor.Emit(OpCodes.Newobj, _threadStartObjectReference);
            ilProcessor.Emit(OpCodes.Newobj, _threadObjectReference);
        }

        private void EmitObjectAccessExpression(ILProcessor ilProcessor, BoundObjectAccessExpression node)
        {
            if (node.Class == null || !node.Class.IsStatic)
            {
                if (node.Expression == null)
                {
                    if (node.Variable is ParameterSymbol parameter)
                    {
                        ilProcessor.Emit(OpCodes.Ldarg,
                            parameter.Ordinal + (inClass != null && !inClass.IsStatic ? 1 : 0));
                    }
                    else if (node.Variable.IsGlobal)
                    {
                        var fieldDefinition = (inType == null ? _globals : _classGlobals[inType])[node.Variable];

                        if (inClass != null && !inClass.IsStatic) ilProcessor.Emit(OpCodes.Ldarg_0);
                        ilProcessor.Emit(
                            inClass == null ? OpCodes.Ldsfld : inClass.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld,
                            fieldDefinition);
                    }
                    else
                    {
                        var variableDefinition = _locals[node.Variable];
                        ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
                    }
                }
                else
                {
                    EmitExpression(ilProcessor, node.Expression);
                }
            }

            if (node.TypeCall != null)
            {
                EmitTypeCallExpression(ilProcessor, node);
                return;
            }

            var classSymbol = node.Class;

            if (node.AccessType == ObjectAccessExpression.AccessType.Call)
            {
                foreach (var argument in node.Arguments)
                    EmitExpression(ilProcessor, argument);

                if (classSymbol.Name == "Main")
                {
                    ilProcessor.Emit(OpCodes.Call , _methods.FirstOrDefault(x => x.Key.Name == node.Function.Name).Value);
                    return;
                }
                
                if (node.Package == null)
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Call : OpCodes.Callvirt, _classMethods[classSymbol][node.Function]);
                else
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Call : OpCodes.Callvirt, _packageClassMethods[classSymbol].FirstOrDefault(x => x.Name == node.Function.Name));
            }
            else if (node.AccessType == ObjectAccessExpression.AccessType.Get)
            {
                if (classSymbol.Name == "Main")
                {
                    ilProcessor.Emit(OpCodes.Ldsfld , _globals.FirstOrDefault(x => x.Key.Name == node.Property.Name).Value);
                    return;
                }
                
                if (node.Package == null)
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, _classGlobals[_classes[classSymbol]].FirstOrDefault(x => x.Key.Name == node.Property.Name && x.Key.Type == node.Property.Type).Value);
                else
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, _packageClassFields[classSymbol].FirstOrDefault(x => x.Name == node.Property.Name));
            }
            else if (node.AccessType == ObjectAccessExpression.AccessType.Set)
            {
                EmitExpression(ilProcessor, node.Value);
                
                if (classSymbol.Name == "Main")
                {
                    ilProcessor.Emit(OpCodes.Stsfld , _globals.FirstOrDefault(x => x.Key.Name == node.Property.Name).Value);
                    return;
                }
                
                if (node.Package == null)
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, _classGlobals[_classes[classSymbol]].FirstOrDefault(x => x.Key.Name == node.Property.Name && x.Key.Type == node.Property.Type).Value);
                else
                    ilProcessor.Emit(classSymbol.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, _packageClassFields[classSymbol].FirstOrDefault(x => x.Name == node.Property.Name));
            }
        }

        private void EmitLiteralExpression(ILProcessor ilProcessor, BoundLiteralExpression node)
        {
            if (node.Type == TypeSymbol.Bool)
            {
                var value = (bool)node.Value;
                var instruction = value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
                ilProcessor.Emit(instruction);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                var value = (int)node.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (node.Type == TypeSymbol.Byte)
            {
                var value = (byte)node.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                var value = (string)node.Value;
                ilProcessor.Emit(OpCodes.Ldstr, value);
            }
            else if (node.Type == TypeSymbol.Float)
            {
                var value = (float)node.Value;
                ilProcessor.Emit(OpCodes.Ldc_R4, value);
            }
            else if (node.Type == TypeSymbol.Any)
            {
                ilProcessor.Emit(OpCodes.Ldnull);
            }
            else
            {
                throw new Exception($"Unexpected literal type: {node.Type}");
            }
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression node)
        {
            try
            {
                if (node.Variable is ParameterSymbol parameter)
                {
                    ilProcessor.Emit(OpCodes.Ldarg, parameter.Ordinal + (inClass != null && !inClass.IsStatic ? 1 : 0));
                }
                else if (node.Variable.IsGlobal)
                {
                    var fieldDefinition = (inType == null ? _globals : _classGlobals[inType])[node.Variable];

                    if (inClass != null && !inClass.IsStatic) ilProcessor.Emit(OpCodes.Ldarg_0);
                    ilProcessor.Emit(inClass == null ? OpCodes.Ldsfld : inClass.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldDefinition);
                }
                else
                {
                    var variableDefinition = _locals[node.Variable];
                    ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
                }

                if (node.isArray)
                {
                    EmitExpression(ilProcessor, node.Index);
                    ilProcessor.Emit(OpCodes.Ldelem_Ref);
                }
            }
            catch
            {
                throw new Exception($"EMITTER ERROR: Could not find Variable / Object '{node.Variable.Name}'");
            }
        }

        private void EmitAssignmentExpression(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            VariableDefinition variableDefinition = null;
            FieldDefinition fieldDefinition = null;

            if (node.Variable.IsGlobal)
                fieldDefinition = (inClass == null ? _globals : _classGlobals[inType])[node.Variable];
            else
                variableDefinition = _locals[node.Variable];

            if (node.isArray)
            {
                if (node.Variable.IsGlobal)
                    ilProcessor.Emit(OpCodes.Ldsfld, fieldDefinition);
                else
                    ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);

                EmitExpression(ilProcessor, node.Index);
                EmitExpression(ilProcessor, node.Expression);

                ilProcessor.Emit(OpCodes.Stelem_Ref);
                return;
            }

            if (inClass != null && !inClass.IsStatic && node.Variable.IsGlobal)
                ilProcessor.Emit(OpCodes.Ldarg_0);

            EmitExpression(ilProcessor, node.Expression);
            //ilProcessor.Emit(OpCodes.Dup);

            if (inType != null)
            {
                if (!node.Variable.IsGlobal)
                {
                    ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
                    return;
                }

                if (inClass.IsStatic)
                    ilProcessor.Emit(OpCodes.Stsfld, fieldDefinition);
                else
                    ilProcessor.Emit(OpCodes.Stfld, fieldDefinition);

                return;
            }

            if (node.Variable.IsGlobal)
                ilProcessor.Emit(OpCodes.Stsfld, fieldDefinition);
            else
                ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private void EmitUnaryExpression(ILProcessor ilProcessor, BoundUnaryExpression node)
        {
            EmitExpression(ilProcessor, node.Operand);

            if (node.Op.Kind == BoundUnaryOperatorKind.Identity)
            {
                // Done
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.LogicalNegation)
            {
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.Negation)
            {
                ilProcessor.Emit(OpCodes.Neg);
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.OnesComplement)
            {
                ilProcessor.Emit(OpCodes.Not);
            }
            else
            {
                throw new Exception($"Unexpected unary operator {SyntaxFacts.GetText(node.Op.SyntaxKind)}({node.Operand.Type})");
            }
        }

        private void EmitBinaryExpression(ILProcessor ilProcessor, BoundBinaryExpression node)
        {
            EmitExpression(ilProcessor, node.Left);
            EmitExpression(ilProcessor, node.Right);

            // +(string, string)

            if (node.Op.Kind == BoundBinaryOperatorKind.Addition)
            {
                if (node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String)
                {
                    ilProcessor.Emit(OpCodes.Call, _stringConcatReference);
                    return;
                }
            }

            // ==(any, any)
            // ==(string, string)

            if (node.Op.Kind == BoundBinaryOperatorKind.Equals)
            {
                if (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                    node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String ||
                    node.Left.Type.isClass && node.Right.Type.isClass)
                {
                    ilProcessor.Emit(OpCodes.Call, _objectEqualsReference);
                    return;
                }
            }

            // !=(any, any)
            // !=(string, string)

            if (node.Op.Kind == BoundBinaryOperatorKind.NotEquals)
            {
                if (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                    node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String ||
                    node.Left.Type.isClass && node.Right.Type.isClass)
                {
                    ilProcessor.Emit(OpCodes.Call, _objectEqualsReference);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    return;
                }
            }

            switch (node.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    ilProcessor.Emit(OpCodes.Add);
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    ilProcessor.Emit(OpCodes.Sub);
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    ilProcessor.Emit(OpCodes.Mul);
                    break;
                case BoundBinaryOperatorKind.Division:
                    ilProcessor.Emit(OpCodes.Div);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalAnd:
                case BoundBinaryOperatorKind.BitwiseAnd:
                    ilProcessor.Emit(OpCodes.And);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalOr:
                case BoundBinaryOperatorKind.BitwiseOr:
                    ilProcessor.Emit(OpCodes.Or);
                    break;
                case BoundBinaryOperatorKind.BitwiseXor:
                    ilProcessor.Emit(OpCodes.Xor);
                    break;
                case BoundBinaryOperatorKind.Equals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Less:
                    ilProcessor.Emit(OpCodes.Clt);
                    break;
                case BoundBinaryOperatorKind.LessOrEquals:
                    ilProcessor.Emit(OpCodes.Cgt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Greater:
                    ilProcessor.Emit(OpCodes.Cgt);
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    ilProcessor.Emit(OpCodes.Clt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new Exception($"Unexpected binary operator {SyntaxFacts.GetText(node.Op.SyntaxKind)}({node.Left.Type}, {node.Right.Type})");
            }
        }

        private void EmitTypeCallExpression(ILProcessor ilProcessor, BoundObjectAccessExpression node)
        {
            if (node.TypeCall.Function == BuiltinFunctions.WriteToClient)
            {
                VariableDefinition var0 = new VariableDefinition(_StreamWriterRef);

                ilProcessor.Body.Variables.Add(var0);

                ilProcessor.Emit(OpCodes.Callvirt, _TCPClientGetStream);
                ilProcessor.Emit(OpCodes.Newobj, _IOStreamWriterCtor);
                ilProcessor.Emit(OpCodes.Stloc, var0);
                ilProcessor.Emit(OpCodes.Ldloc, var0);
                EmitExpression(ilProcessor, node.TypeCall.Arguments[0]);
                ilProcessor.Emit(OpCodes.Callvirt, _IOWriteLine);
                ilProcessor.Emit(OpCodes.Ldloc, var0);
                ilProcessor.Emit(OpCodes.Callvirt, _IOFlush);
                return;
            }
            else if (node.TypeCall.Function == BuiltinFunctions.ReadClient)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPClientGetStream);
                ilProcessor.Emit(OpCodes.Newobj, _IOStreamReaderCtor);
                ilProcessor.Emit(OpCodes.Callvirt, _IOReadLine);
                return;
            }
            else if (node.TypeCall.Function == BuiltinFunctions.WriteToSocket)
            {
                VariableDefinition var0 = new VariableDefinition(_StreamWriterRef);

                ilProcessor.Body.Variables.Add(var0);

                ilProcessor.Emit(OpCodes.Newobj, _TCPNetworkStreamCtor);
                ilProcessor.Emit(OpCodes.Newobj, _IOStreamWriterCtor);
                ilProcessor.Emit(OpCodes.Stloc, var0);
                ilProcessor.Emit(OpCodes.Ldloc, var0);
                EmitExpression(ilProcessor, node.TypeCall.Arguments[0]);
                ilProcessor.Emit(OpCodes.Callvirt, _IOWriteLine);
                ilProcessor.Emit(OpCodes.Ldloc, var0);
                ilProcessor.Emit(OpCodes.Callvirt, _IOFlush);
                return;
            }
            else if (node.TypeCall.Function == BuiltinFunctions.ReadSocket)
            {
                ilProcessor.Emit(OpCodes.Newobj, _TCPNetworkStreamCtor);
                ilProcessor.Emit(OpCodes.Newobj, _IOStreamReaderCtor);
                ilProcessor.Emit(OpCodes.Callvirt, _IOReadLine);
                return;
            }

            foreach (var argument in node.TypeCall.Arguments)
                EmitExpression(ilProcessor, argument);

            if (node.TypeCall.Function == BuiltinFunctions.GetLength)
            {
                var nameSpaceRef = ResolveMethodPublic(_knownTypes[node.Variable.Type].FullName, "get_Length", Array.Empty<string>());
                ilProcessor.Emit(OpCodes.Callvirt, nameSpaceRef);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.Substring)
            {
                var nameSpaceRef = ResolveMethodPublic(_knownTypes[node.Variable.Type].FullName, "Substring", new[] { "System.Int32", "System.Int32" });
                ilProcessor.Emit(OpCodes.Callvirt, nameSpaceRef);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.StartThread)
            {
                var nameSpaceRef = ResolveMethodPublic(_knownTypes[node.Variable.Type].FullName, "Start", Array.Empty<string>());
                ilProcessor.Emit(OpCodes.Callvirt, nameSpaceRef);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.KillThread)
            {
                var nameSpaceRef = ResolveMethodPublic(_knownTypes[node.Variable.Type].FullName, "Interrupt", Array.Empty<string>());
                ilProcessor.Emit(OpCodes.Callvirt, nameSpaceRef);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.GetArrayLength)
            {
                ilProcessor.Emit(OpCodes.Ldlen);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.OpenSocket)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPAcceptSocketReference);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.CloseClient)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPClientClose);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.CloseSocket)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPSocketClose);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.IsClientConnected)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPClientConnected);
            }
            else if (node.TypeCall.Function == BuiltinFunctions.IsSocketConnected)
            {
                ilProcessor.Emit(OpCodes.Callvirt, _TCPSocketConnected);
            }
            else
            {
                throw new Exception("Couldnt find TypeFunction: " + node.TypeCall.Function.Name);
            }
        }

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression node)
        {
            if (inType != null && !inClass.IsStatic && _classMethods[_classes.FirstOrDefault(x => x.Value == inType).Key].ContainsKey(node.Function))
            {
                ilProcessor.Emit(OpCodes.Ldarg_0);
            }

            foreach (var argument in node.Arguments)
                EmitExpression(ilProcessor, argument);

            if (node.Function == BuiltinFunctions.Die)
                ilProcessor.Emit(OpCodes.Call, _envDie);
            else if (node.Function == BuiltinFunctions.Borger)
            {
                ilProcessor.Emit(OpCodes.Ldstr, "borger");
                ilProcessor.Emit(OpCodes.Call, _consoleWriteLineReference);
            }
            else
            {
                if (node.Function.Package != "")
                {
                    List<string> args = new List<string>();

                    foreach (ParameterSymbol p in _packages[node.Function.Package].scope.GetDeclaredFunctions().FirstOrDefault(x => x.Name == node.Function.Name).Parameters)
                        args.Add(_knownTypes[p.Type].FullName);

                    var method = ResolveMethodPublic(node.Function.Package + "." + node.Function.Package, node.Function.Name, args.ToArray());
                    ilProcessor.Emit(OpCodes.Call, method);
                    return;
                }

                var methodDefinition = (inType == null ? _methods : _classMethods[_classes.FirstOrDefault(x => x.Value == inType).Key])[node.Function];
                ilProcessor.Emit(OpCodes.Call, methodDefinition);
            }
        }

        private void EmitRandomField()
        {
            _randomFieldDefinition = new FieldDefinition(
                "$rnd",
                FieldAttributes.Static | FieldAttributes.Private,
                _randomReference
            );
            _typeDefinition.Fields.Add(_randomFieldDefinition);

            var staticConstructor = new MethodDefinition(
                ".cctor",
                MethodAttributes.Static |
                MethodAttributes.Private |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                _knownTypes[TypeSymbol.Void]
            );
            _typeDefinition.Methods.Insert(0, staticConstructor);

            var ilProcessor = staticConstructor.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Newobj, _randomCtorReference);
            ilProcessor.Emit(OpCodes.Stsfld, _randomFieldDefinition);
            ilProcessor.Emit(OpCodes.Ret);
        }

        private void EmitConversionExpression(ILProcessor ilProcessor, BoundConversionExpression node)
        {
            EmitExpression(ilProcessor, node.Expression);
            var needsBoxing = node.Expression.Type == TypeSymbol.Bool ||
                              node.Expression.Type == TypeSymbol.Int ||
                              node.Expression.Type == TypeSymbol.Float ||
                              node.Expression.Type == TypeSymbol.AnyArr ||
                              node.Expression.Type == TypeSymbol.IntArr ||
                              node.Expression.Type == TypeSymbol.FloatArr ||
                              node.Expression.Type == TypeSymbol.StringArr ||
                              node.Expression.Type == TypeSymbol.BoolArr ||
                              node.Expression.Type == TypeSymbol.ThreadArr ||
                              node.Expression.Type == TypeSymbol.TCPClientArr ||
                              node.Expression.Type == TypeSymbol.TCPListenerArr ||
                              node.Expression.Type == TypeSymbol.TCPSocketArr ||
                              node.Expression.Type.isClass;
            if (needsBoxing)
                ilProcessor.Emit(OpCodes.Box, _knownTypes[node.Expression.Type.isClass ? _knownTypes.Keys.FirstOrDefault(x => x.Name == node.Expression.Type.Name) : node.Expression.Type]);

            if (node.Type == TypeSymbol.Any)
            {
                // Done
            }
            else if (node.Type == TypeSymbol.Bool)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToBooleanReference);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToInt32Reference);
            }
            else if (node.Type == TypeSymbol.Byte)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToUInt8Reference);
            }
            else if (node.Type == TypeSymbol.Float)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToSingleReference);
            }
            else if (node.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToStringReference);
            }
            else if (node.Type == TypeSymbol.TCPClient)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPClient]);
            }
            else if (node.Type == TypeSymbol.TCPListener)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPListener]);
            }
            else if (node.Type == TypeSymbol.TCPSocket)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPSocket]);
            }
            else if (node.Type == TypeSymbol.AnyArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.AnyArr]);
            }
            else if (node.Type == TypeSymbol.IntArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.IntArr]);
            }
            else if (node.Type == TypeSymbol.FloatArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.FloatArr]);
            }
            else if (node.Type == TypeSymbol.StringArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.StringArr]);
            }
            else if (node.Type == TypeSymbol.BoolArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.BoolArr]);
            }
            else if (node.Type == TypeSymbol.ThreadArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.ThreadArr]);
            }
            else if (node.Type == TypeSymbol.TCPClientArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPClientArr]);
            }
            else if (node.Type == TypeSymbol.TCPListenerArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPListenerArr]);
            }
            else if (node.Type == TypeSymbol.TCPSocketArr)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes[TypeSymbol.TCPSocketArr]);
            }
            else if (node.Type.isClass)
            {
                ilProcessor.Emit(OpCodes.Castclass, _knownTypes.FirstOrDefault(x => x.Key.Name == node.Type.Name).Value);
            }
            else
            {
                throw new Exception($"Unexpected convertion from {node.Expression.Type} to {node.Type}");
            }
        }
    }
}