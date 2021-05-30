using ReCT.CodeAnalysis.Symbols;

namespace ReCT.CodeAnalysis.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(exists: false, isIdentity: false, isImplicit: false);
        public static readonly Conversion Identity = new Conversion(exists: true, isIdentity: true, isImplicit: true);
        public static readonly Conversion Implicit = new Conversion(exists: true, isIdentity: false, isImplicit: true);
        public static readonly Conversion Explicit = new Conversion(exists: true, isIdentity: false, isImplicit: false);

        private Conversion(bool exists, bool isIdentity, bool isImplicit)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsImplicit = isImplicit;
        }

        public bool Exists { get; }
        public bool IsIdentity { get; }
        public bool IsImplicit { get; }
        public bool IsExplicit => Exists && !IsImplicit;

        public static Conversion Classify(TypeSymbol from, TypeSymbol to)
        {
            if (from == to)
                return Conversion.Identity;

            if (from.isClass && to.isClass && from.Name == to.Name)
                return Conversion.Identity;

            if (from != TypeSymbol.Void && to == TypeSymbol.Any)
            {
                return Conversion.Implicit;
            }

            if (from == TypeSymbol.Any && to != TypeSymbol.Void)
            {
                return Conversion.Explicit;
            }

            if (from.isEnum)
                if (to == TypeSymbol.Int)
                    return Conversion.Implicit;

            if (from.isEnum)
                if (to == TypeSymbol.String)
                    return Conversion.Explicit;

            if (from == TypeSymbol.Int)
                if (to.isEnum)
                    return Conversion.Explicit;

            if (from == TypeSymbol.String)
                if (to.isEnum)
                    return Conversion.Explicit;

            if (from == TypeSymbol.Int)
                if (to == TypeSymbol.Byte)
                    return Conversion.Implicit;

            if (from == TypeSymbol.Byte)
                if (to == TypeSymbol.Int)
                    return Conversion.Implicit;

            if (from == TypeSymbol.Int)
                if (to == TypeSymbol.Float)
                    return Conversion.Implicit;

            if (from == TypeSymbol.Float)
                if (to == TypeSymbol.Int)
                    return Conversion.Explicit;

            if (from == TypeSymbol.AnyArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.AnyArr)
                return Conversion.Explicit;

            if (from == TypeSymbol.BoolArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.BoolArr)
                return Conversion.Explicit;
            if (from == TypeSymbol.IntArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.IntArr)
                return Conversion.Explicit;
            if (from == TypeSymbol.ByteArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.ByteArr)
                return Conversion.Explicit;
            if (from == TypeSymbol.FloatArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.FloatArr)
                return Conversion.Explicit;
            if (from == TypeSymbol.ThreadArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.ThreadArr)
                return Conversion.Explicit;
            if (from == TypeSymbol.StringArr && to == TypeSymbol.Any)
                return Conversion.Explicit;
            if (from == TypeSymbol.Any && to == TypeSymbol.StringArr)
                return Conversion.Explicit;

            if (from == TypeSymbol.Bool || from == TypeSymbol.Int || from == TypeSymbol.Float || from == TypeSymbol.Byte)
            {
                if (to == TypeSymbol.String)
                    return Conversion.Explicit;
            }

            if (from == TypeSymbol.String)
            {
                if (to == TypeSymbol.Bool || to == TypeSymbol.Int || to == TypeSymbol.Float || to == TypeSymbol.Byte)
                    return Conversion.Explicit;
            }

            return Conversion.None;
        }
    }
}
