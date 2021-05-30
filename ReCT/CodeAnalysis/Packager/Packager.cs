﻿using System;
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

            List<string> TypesInThisPackage = new List<string>();
            Dictionary<string, ClassSymbol> ClassesInThisPackage = new Dictionary<string, ClassSymbol>();

            if (TypeSymbol.Class == null) {TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>(); }

            foreach (TypeDefinition t in types)
            {
                TypesInThisPackage.Add(t.Name);

                var classSymbol = new ClassSymbol(t.Name, null, t.IsSealed && t.IsAbstract);
                var classTypeSymbol = new TypeSymbol(classSymbol.Name);
                classTypeSymbol.isClass = true;
                TypeSymbol.Class.Add(classSymbol, classTypeSymbol);

                if (!classSymbol.IsStatic)
                {
                    var classArraySymbol = new TypeSymbol(classSymbol.Name + "Arr");
                    classArraySymbol.isClass = true;
                    classArraySymbol.isClassArray = true;
                    TypeSymbol.Class.Add(new ClassSymbol(classSymbol.Name + "Arr", null, false), classArraySymbol);
                }
                ClassesInThisPackage.Add(t.Name, classSymbol);
            }

            foreach (TypeDefinition t in types)
            {
                var classSymbol = ClassesInThisPackage[t.Name];
                var classMethods = t.Methods;
                var classFields = t.Fields;
                classSymbol.Scope = new BoundScope(scope);

                Dictionary<string, FunctionSymbol> varGetters = new Dictionary<string, FunctionSymbol>();
                Dictionary<string, FunctionSymbol> varSetters = new Dictionary<string, FunctionSymbol>();
                
                foreach (MethodDefinition m in classMethods)
                {
                    if (!m.IsPublic)
                        continue;

                    var parameters = ImmutableArray.CreateBuilder<Symbols.ParameterSymbol>();

                    foreach (ParameterDefinition p in m.Parameters)
                    {
                        var parameterName = p.Name;
                        var parameterType = Binding.Binder.LookupType(netTypeLookup(p.ParameterType.Name.ToLower(), TypesInThisPackage.ToArray()));

                        var parameter = new Symbols.ParameterSymbol(parameterName, parameterType, parameters.Count);
                        parameters.Add(parameter);
                    }

                    var returnType = netTypeLookup(m.ReturnType.Name.ToLower(), TypesInThisPackage.ToArray());

                    //Console.WriteLine("FUNCTON: " + m.Name + "; TYPE: " + t.Name);

                    var methodType = Binder.LookupType(returnType);

                    var symbol = new Symbols.FunctionSymbol(m.Name == ".ctor" ? "Constructor" : m.Name, parameters.ToImmutable(), methodType, package: key);
                    classSymbol.Scope.TryDeclareFunction(symbol);

                    if (m.Name.StartsWith("get_") && m.Parameters.Count == 0)
                    {
                        //Console.WriteLine("Found function: " + m.Name + "; adding Getter: " + m.Name.Substring(4));
                        varGetters.Add(m.Name.Substring(4), symbol);
                    }

                    if (m.Name.StartsWith("set_") && m.Parameters.Count == 1)
                    {
                        //Console.WriteLine("Found function: " + m.Name + "; adding Setter: " + m.Name.Substring(4));
                        varSetters.Add(m.Name.Substring(4), symbol);
                    }
                }

                foreach (FieldDefinition f in classFields)
                {
                    if (!f.IsPublic || f.IsPrivate)
                        continue;

                    var type = netTypeLookup(f.FieldType.Name.ToLower(), TypesInThisPackage.ToArray());
                    classSymbol.Scope.TryDeclareVariable(new GlobalVariableSymbol(f.Name, false, Binder.LookupType(type)));
                }

                foreach (var get in varGetters)
                {
                    bool isReadonly = !varSetters.Keys.Contains(get.Key);
                    classSymbol.Scope.TryDeclareVariable(new FunctionalVariableSymbol(get.Key, isReadonly, get.Value.Type, false));
                    //Console.WriteLine("Declaring FunctionalVariable: " + get.Key + "; Readonly: " + isReadonly);
                }
                
                if (TypeSymbol.Class == null) TypeSymbol.Class = new Dictionary<ClassSymbol, TypeSymbol>();

                if (!TypeSymbol.Class.ContainsKey(classSymbol))
                {
                    var typesymbol = new TypeSymbol(classSymbol.Name);
                    typesymbol.isClass = true;
                    TypeSymbol.Class.Add(classSymbol, typesymbol);

                    if (!classSymbol.IsStatic)
                    {
                        var arraysymbol = new TypeSymbol(classSymbol.Name + "Arr");
                        arraysymbol.isClass = true;
                        arraysymbol.isClassArray = true;
                        TypeSymbol.Class.Add(new ClassSymbol(classSymbol.Name + "Arr"), arraysymbol);
                    }
                }

                scope.TryDeclareClass(classSymbol);
            }

            foreach (MethodDefinition m in methods)
            {
                if (!m.IsPublic)
                    continue;

                var parameters = ImmutableArray.CreateBuilder<Symbols.ParameterSymbol>();

                foreach (ParameterDefinition p in m.Parameters)
                {
                    var parameterName = p.Name;
                    var parameterType = Binding.Binder.LookupType(netTypeLookup(p.ParameterType.Name.ToLower(), TypesInThisPackage.ToArray()));

                    var parameter = new Symbols.ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }

                var returnType = netTypeLookup(m.ReturnType.Name.ToLower(), TypesInThisPackage.ToArray());

                //Console.WriteLine("FUNCTON: " + m.Name + "; TYPE: " + m.MethodReturnType.Name + "; RTYPE: " + m.ReturnType.Name);

                var methodType = Binder.LookupType(returnType);

                scope.TryDeclareFunction(new Symbols.FunctionSymbol(m.Name, parameters.ToImmutable(), methodType, package: key));
            }
            
            return new Package(key, sysPack, scope);
        }

        static string netTypeLookup(string netversion, string[] inThisPackage)
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
                default:
                    if (netversion.EndsWith("[]"))
                        return netTypeLookup(netversion.Replace("[]", ""), inThisPackage) + "Arr";
                    if (inThisPackage.FirstOrDefault(x => x.ToLower() == netversion) != null)
                        return inThisPackage.FirstOrDefault(x => x.ToLower() == netversion);

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
