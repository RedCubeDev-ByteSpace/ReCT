using System;
using System.Diagnostics;
using System.Threading;

namespace sys
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ReCTs standard system package ^^");
        }
    }

    //This is the Official ReCT SYS Package -- ©2021 RedCube
    public static class sys
    {
        public static void Print(string text)
        {
            Console.WriteLine(text);
        }

        public static void Write(string text)
        {
            Console.Write(text);
        }

        public static string Input()
        {
            return Console.ReadLine();
        }

        public static string InputKey()
        {
            return Console.ReadKey().KeyChar.ToString();
        }

        public static string InputAction()
        {
            return Console.ReadKey(true).KeyChar.ToString();
        }

        public static void Clear()
        {
            Console.Clear();
        }

        public static void Sleep(int mills)
        {
            Thread.Sleep(mills);
        }

        public static void Beep(int freq, int duration)
        {
            Console.Beep(freq, duration);
        }

        public static string Char(int ascii)
        {
            return ((char)ascii).ToString();
        }
        
        public static string Arg(int index)
        {
            return Environment.GetCommandLineArgs()[index];
        }

        public static string[] Args()
        {
            return Environment.GetCommandLineArgs();
        } 

        public static void SetSize(int w, int h)
        {
            Console.SetWindowSize(w,h);
        }

        public static int GetSizeX()
        {
            return Console.WindowWidth;
        }

        public static int GetSizeY()
        {
            return Console.WindowHeight;
        }

        public static void SetCursor(int x, int y)
        {
            Console.SetCursorPosition(x,y);
        }

        public static int GetCursorX()
        {
            return Console.CursorLeft;
        }

        public static int GetCursorY()
        {
            return Console.CursorTop;
        }

        public static void SetCursorVisible(bool visible)
        {
            Console.CursorVisible = visible;
        }

        public static bool GetCursorVisible()
        {
            return Console.CursorVisible;
        }

        public static void SetConsoleForeground(int color)
        {
            Console.ForegroundColor = (ConsoleColor)color;
        }

        public static void SetConsoleBackground(int color)
        {
            Console.BackgroundColor = (ConsoleColor)color;
        }

        public static void LaunchApplication(string application)
        {
            Process.Start(application);
        }

        public static void LaunchApplicationWithArgs(string application, string args, bool wait)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = application;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.Arguments = args;

            var proc = Process.Start(psi);
            if (wait) proc.WaitForExit();
        }


        public static string LaunchApplicationWithOutput(string application, string args)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = application;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.Arguments = args;

            var proc = Process.Start(psi);
            proc.WaitForExit();
            string normalOut = proc.StandardOutput.ReadToEnd();
            string errorOut = proc.StandardError.ReadToEnd();

            return normalOut + errorOut;
        }
    }
}
