using System;

namespace math
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("good old meth lib.");
        }
    }

    //This is the Official ReCT MATH Package -- ©2021 RedCube
    public static class math
    {
        private static Random rnd;

        public static int Random(int max)
        {
            return rnd.Next(max);
        }

        public static int RandomRange(int min, int max)
        {
            return rnd.Next(min, max);
        }

        public static int Floor(float val)
        {
            return (int)Math.Floor(val);
        }

        public static int Ceil(float val)
        {
            return (int)Math.Ceiling(val);
        }



        public static float Sin(float val)
        {
            return (float)Math.Sin(val);
        }

        public static float Cos(float val)
        {
            return (float)Math.Cos(val);
        }

        public static float Tan(float val)
        {
            return (float)Math.Tan(val);
        }

        public static float ASin(float val)
        {
            return (float)Math.Asin(val);
        }

        public static float ACos(float val)
        {
            return (float)Math.Acos(val);
        }

        public static float ATan(float val)
        {
            return (float)Math.Atan(val);
        }



        public static float SinDeg(float val)
        {
            return (float)Math.Sin(val * 0.0174533f);
        }

        public static float CosDeg(float val)
        {
            return (float)Math.Cos(val * 0.0174533f);
        }

        public static float TanDeg(float val)
        {
            return (float)Math.Tan(val * 0.0174533f);
        }

        public static float ASinDeg(float val)
        {
            return (float)Math.Asin(val) / 0.0174533f;
        }

        public static float ACosDeg(float val)
        {
            return (float)Math.Acos(val) / 0.0174533f;
        }

        public static float ATanDeg(float val)
        {
            return (float)Math.Atan(val) / 0.0174533f;
        }

        public static float Pi()
        {
            return (float)Math.PI;
        }

        public static float Sqrt(float a)
        {
            return (float)Math.Sqrt(a);
        }

        public static float Pow(float a, float b)
        {
            return (float)Math.Pow(a, b);
        }

        public static float Clamp(float val, float min, float max)
        {
            return Math.Clamp(val, min, max);
        }

        public static float Abs(float val)
        {
            return Math.Abs(val);
        }
    }
}
