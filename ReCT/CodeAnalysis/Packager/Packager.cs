using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
using System.Collections.Immutable;
using System.Linq;

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

            sysPack = "Packages/" + sysPack;

            Assembly Asm = Assembly.LoadFrom(sysPack);
            var Asmtype = Asm.GetTypes()[0];

            Console.WriteLine($"Loading Package '{sysPack} [{Asmtype}]'...");

            var methods = Asmtype.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (MethodInfo m in methods)
            {
                var parameters = ImmutableArray.CreateBuilder<Symbols.ParameterSymbol>();

                foreach (ParameterInfo p in m.GetParameters())
                {
                    var parameterName = p.Name;
                    var parameterType = Binding.Binder.LookupType(p.GetType().Name);

                    var parameter = new Symbols.ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }

                scope.TryDeclareFunction(new Symbols.FunctionSymbol(m.Name, parameters.ToImmutable(), Binding.Binder.LookupType(m.GetType().Name), package: sysPack));
            }

            return new Package(systemPackages.FirstOrDefault(x => x.Value == sysPack).Key, sysPack, scope);
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
