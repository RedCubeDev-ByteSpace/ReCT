using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;

namespace ReCT.CodeAnalysis.Package
{
    public static class Packager
    {
        public static IDictionary<string, string> systemPackages = new Dictionary<string, string>()
        {
            {"sys","ReCT.sys.pack"},
            {"math","ReCT.math.pack"},
        };

        public static Package loadPackage(string sysPack)
        {
            var scope = new Binding.BoundScope(null);
            var key = systemPackages.FirstOrDefault(x => x.Value == sysPack).Key;

            sysPack = "Packages/" + sysPack;

            if (!File.Exists(sysPack))
                Console.WriteLine($"Couldnt find file '{sysPack}'!");

            AssemblyDefinition Asm = AssemblyDefinition.ReadAssembly(sysPack);
            TypeDefinition AsmType = Asm.MainModule.Types.FirstOrDefault(x => x.Name == key);

            Console.WriteLine($"Loading Package '{sysPack} [{AsmType.Namespace}]'...");

            var methods = AsmType.Methods;

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

                var methodType = Binding.Binder.LookupType(returnType);

                scope.TryDeclareFunction(new Symbols.FunctionSymbol(m.Name, parameters.ToImmutable(), methodType, package: key));
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
                case "threading.thread":
                    return "thread";
                case "net.sockets.tcpclient":
                    return "tcpclient";
                case "net.sockets.tcplistener":
                    return "tcplistener";
                case "net.sockets.socket":
                    return "tcpscoket";
                default:
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
