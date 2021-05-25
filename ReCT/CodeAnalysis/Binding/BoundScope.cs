using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    public sealed class BoundScope
    {
        private Dictionary<string, Symbol> _symbols;
        public string Name;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
        }
        public BoundScope(BoundScope parent, string name)
        {
            Parent = parent;
            Name = name;
        }

        public BoundScope Parent { get; }

        public bool TryDeclareVariable(VariableSymbol variable)
            => TryDeclareSymbol(variable);

        public bool TryDeclareFunction(FunctionSymbol function)
            => TryDeclareSymbol(function);

        public bool TryDeclareClass(ClassSymbol _class)
            => TryDeclareSymbol(_class);

        public bool TryDeclareEnum(EnumSymbol _enum)
            => TryDeclareSymbol(_enum);

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol)
            where TSymbol : Symbol
        {
            if (_symbols == null)
                _symbols = new Dictionary<string, Symbol>();
            else if (_symbols.ContainsKey(symbol.Name))
                return false;

            _symbols.Add(symbol.Name, symbol);
            return true;
        }

        public Symbol TryLookupSymbol(string name, bool noParent = false)
        {
            if (_symbols != null && _symbols.TryGetValue(name, out var symbol))
                return symbol;

            if (noParent) return null;
            return Parent?.TryLookupSymbol(name);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
            => GetDeclaredSymbols<VariableSymbol>();

        public ImmutableArray<ClassSymbol> GetDeclaredClasses()
            => GetDeclaredSymbols<ClassSymbol>();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
            => GetDeclaredSymbols<FunctionSymbol>();

        public ImmutableArray<EnumSymbol> GetDeclaredEnums()
            => GetDeclaredSymbols<EnumSymbol>();

        private ImmutableArray<TSymbol> GetDeclaredSymbols<TSymbol>()
            where TSymbol : Symbol
        {
            if (_symbols == null)
                return ImmutableArray<TSymbol>.Empty;

            return _symbols.Values.OfType<TSymbol>().ToImmutableArray();
        }

        internal void ClearVariables()
        {
            if (_symbols == null) return;

            Queue<KeyValuePair<string, Symbol>> symsToDed = new Queue<KeyValuePair<string, Symbol>>();

            foreach (var sym in _symbols)
            {
                if (sym.Value is VariableSymbol)
                    symsToDed.Enqueue(sym);
            }

            while (symsToDed.Count > 0)
            {
                _symbols.Remove(symsToDed.Dequeue().Key);
            }
        }
    }
}
