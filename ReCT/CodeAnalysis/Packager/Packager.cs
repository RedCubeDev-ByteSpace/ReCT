using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;
using ReCT.CodeAnalysis.Binding;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Package
{
    public static class Packager
    {
        public static IDictionary<string, string> systemPackages = new Dictionary<string, string>()
        {
            {"sys", "ReCT.sys.pack" },
            {"math","ReCT.math.pack"},
            {"net","ReCT.net.pack"},
            {"web","ReCT.web.pack"},
            {"io","ReCT.io.pack"},
            {"audio","ReCT.audio.pack"},
        };

        public static Package loadPackage(string sysPack, bool isDLL)
        {
            var scope = new Binding.BoundScope(null);
            var key = systemPackages.FirstOrDefault(x => x.Value == sysPack).Key;
            if (isDLL) key = Path.GetFileNameWithoutExtension(sysPack);

            sysPack = "Packages/" + sysPack;

            AssemblyDefinition Asm = AssemblyDefinition.ReadAssembly(sysPack);
            TypeDefinition AsmType = Asm.MainModule.Types.FirstOrDefault(x => x.Name == key);

            var methods = AsmType.Methods;
            var types = AsmType.NestedTypes;

            foreach (MethodDefinition m in methods)
            {
                if (!m.IsPublic)
                    continue;

                var parameters = ImmutableArray.CreateBuilder<Symbols.ParameterSymbol>();

                foreach (ParameterDefinition p in m.Parameters)
                {
                    var parameterName = p.Name;
                    var parameterType = Binding.Binder.LookupType(netTypeLookup(p.ParameterType.Name.ToLower()));

                    var parameter = new Symbols.ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }

                var returnType = netTypeLookup(m.ReturnType.Name.ToLower());

                //Console.WriteLine("FUNCTON: " + m.Name + "; TYPE: " + m.MethodReturnType.Name + "; RTYPE: " + m.ReturnType.Name);

                var methodType = Binder.LookupType(returnType);

                scope.TryDeclareFunction(new Symbols.FunctionSymbol(m.Name, parameters.ToImmutable(), methodType, package: key));
            }

            foreach (TypeDefinition t in types)
            {
                var classSymbol = new ClassSymbol(t.Name, null, t.IsSealed && t.IsAbstract);
                var classMethods = t.Methods;
                var classFields = t.Fields;
                classSymbol.Scope = new BoundScope(scope);

                foreach (MethodDefinition m in classMethods)
                {
                    var parameters = ImmutableArray.CreateBuilder<Symbols.ParameterSymbol>();

                    foreach (ParameterDefinition p in m.Parameters)
                    {
                        var parameterName = p.Name;
                        var parameterType = Binding.Binder.LookupType(netTypeLookup(p.ParameterType.Name.ToLower()));

                        var parameter = new Symbols.ParameterSymbol(parameterName, parameterType, parameters.Count);
                        parameters.Add(parameter);
                    }

                    var returnType = netTypeLookup(m.ReturnType.Name.ToLower());

                    //Console.WriteLine("FUNCTON: " + m.Name + "; TYPE: " + t.Name);

                    var methodType = Binder.LookupType(returnType);

                    classSymbol.Scope.TryDeclareFunction(new Symbols.FunctionSymbol(m.Name == ".ctor" ? "Constructor" : m.Name, parameters.ToImmutable(), methodType, package: key));
                }

                foreach (FieldDefinition f in classFields)
                {
                    var type = netTypeLookup(f.FieldType.Name.ToLower());
                    classSymbol.Scope.TryDeclareVariable(new GlobalVariableSymbol(f.Name, false, Binder.LookupType(type)));
                }

                if (TypeSymbol.Class == null) TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>();

                if (!TypeSymbol.Class.ContainsKey(classSymbol))
                {
                    var typesymbol = new TypeSymbol(classSymbol.Name);
                    typesymbol.isClass = true;
                    TypeSymbol.Class.Add(classSymbol, typesymbol);
                    
                    var arraysymbol = new TypeSymbol(classSymbol.Name + "Arr");
                    arraysymbol.isClass = true;
                    TypeSymbol.Class.Add(new ClassSymbol(classSymbol.Name + "Arr"), arraysymbol);
                }

                scope.TryDeclareClass(classSymbol);
            }

            return new Package(key, sysPack, scope);
        }

        static string netTypeLookup(string netversion)
        {
            switch (netversion)
            {
                case "object":
                    return "any";
                case "boolean":
                    return "bool";
                case "int32":
                    return "int";
                case "single":
                    return "float";
                case "byte":
                    return "byte";
                case "string":
                    return "string";
                case "void":
                case "":
                    return "void";
                case "thread":
                    return "thread";
                case "tcpclient":
                    return "tcpclient";
                case "tcplistener":
                    return "tcplistener";
                case "socket":
                    return "tcpscoket";
                default:
                    if (netversion.EndsWith("[]"))
                        return netTypeLookup(netversion.Replace("[]", "")) + "Arr";

                    throw new Exception($"Couldnt find .Net type '{netversion}'");
            }
        }
    }
    public class Package
    {
        public Package(string Name, string FullName, Binding.BoundScope Scope)
        {
            name = Name;
            fullName = FullName;
            scope = Scope;
        }

        public string name;
        public string fullName;
        public Binding.BoundScope scope;
    }
}
