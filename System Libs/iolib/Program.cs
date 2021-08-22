using System;
using System.IO;

namespace io
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("this sure is one of the more boring libraries ;D");
        }
    }

    //This is the Official ReCT IO Package -- ©2021 RedCube
    public static class io
    {
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }

        public static void WriteFile(string path, string text)
        {
            File.WriteAllText(path, text);
        }

        public static void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public static void CopyFile(string from, string to)
        {
            File.Copy(from, to);
        }
        
        public static int FileSize(string path)
        {
            return new System.IO.FileInfo(path).Length;
        }

        public static string[] GetFilesInDirectory(string path)
        {
            return Directory.GetFiles(path);
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void DeleteDirectory(string path)
        {
            Directory.Delete(path);
        }

        public static string[] GetDirsInDirectory(string path)
        {
            return Directory.GetDirectories(path);
        }

        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public static void ChangeDirectory(string path)
        {
            Directory.SetCurrentDirectory(path);
        }
    }
}
