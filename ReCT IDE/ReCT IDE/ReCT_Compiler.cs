using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReCT;
using ReCT.CodeAnalysis;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

namespace ReCT_IDE
{
    public class ReCT_Compiler
    {
        public string Variables = "";
        public Diagnostic[] errors = new Diagnostic[0];

        public void Check(string code)
        {
            var syntaxTree = SyntaxTree.Parse(code);
            var compilation = Compilation.Create(syntaxTree);

            //var eval = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            //errors = eval.Diagnostics.ToArray();

            Variables = "";

            var vars = compilation.Variables.ToArray();
            foreach(VariableSymbol vs in vars)
            {
                Variables += vs.Name + "|";
            }
            if(Variables != "")
            {
                Variables = Variables.Substring(0, Variables.Length - 1);
                Variables = "(" + Variables + ")";
            }
        }
        public void CompileRCTBC(string fileOut, string code)
        {
            var syntaxTree = SyntaxTree.Parse(code);
            var compilation = Compilation.Create(syntaxTree);
            var errors = compilation.Emit(Path.GetFileNameWithoutExtension(fileOut), new string[] { @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Console.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.Extensions.dll" }, Path.GetDirectoryName(fileOut) + "\\" + Path.GetFileNameWithoutExtension(fileOut) + ".dll"); ;

            if (errors.Any())
                Console.WriteLine("oof");

            //generate runtimeconfig
            using (StreamWriter sw = new StreamWriter(new FileStream(Path.GetDirectoryName(fileOut) + "\\" + Path.GetFileNameWithoutExtension(fileOut) + ".runtimeconfig.json", FileMode.Create)))
            {
                sw.Write("{\"runtimeOptions\": {\"tfm\": \"netcoreapp3.1\",\"framework\": {\"name\": \"Microsoft.NETCore.App\",\"version\": \"3.1.0\"}}}");
            }
            using (StreamWriter sw = new StreamWriter(new FileStream(fileOut, FileMode.Create)))
            {
                sw.Write($"dotnet exec {Path.GetFileNameWithoutExtension(fileOut)}.dll");
            }

        }
        public void CompileDNCLI(string fileName, string outName)
        {
            if (Directory.Exists(@"Builder/ReCTClasses"))
            {
                ForceDeleteFilesAndFoldersRecursively(@"Builder/ReCTClasses");
                Directory.CreateDirectory(@"Builder/ReCTClasses");
            }

            if (!Directory.Exists(@"Builder/ReCTClasses"))
                Directory.CreateDirectory(@"Builder/ReCTClasses");

            using (StreamWriter sw = new StreamWriter(new FileStream($"Builder/ReCTClasses/{Path.GetFileNameWithoutExtension(outName)}.rcproj", FileMode.Create)))
            {
                sw.Write("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp3.1</TargetFramework></PropertyGroup></Project>");
                sw.Flush();
                sw.Close();
            }
            File.Copy(fileName, $"Builder/ReCTClasses/{Path.GetFileName(fileName)}");

            string strCmdText;
            strCmdText = $"/C cd \"{Path.GetFullPath($"Builder/ReCTClasses")}\" & dotnet build";
            var process = Process.Start("CMD.exe", strCmdText);
            process.WaitForExit();

            var outDir = Path.GetDirectoryName(outName);

            File.Copy($"Builder/ReCTClasses/bin/Debug/netcoreapp3.1/{Path.GetFileNameWithoutExtension(outName)}.exe", outName, true);
            File.Copy($"Builder/ReCTClasses/bin/Debug/netcoreapp3.1/{Path.GetFileNameWithoutExtension(outName)}.runtimeconfig.json", outDir + $"/{Path.GetFileNameWithoutExtension(outName)}.runtimeconfig.json", true);
            File.Copy($"Builder/ReCTClasses/bin/Debug/netcoreapp3.1/{Path.GetFileNameWithoutExtension(outName)}.dll", outDir + $"/{Path.GetFileNameWithoutExtension(outName)}.dll", true);
        }

        public static void ForceDeleteFilesAndFoldersRecursively(string target_dir)
        {
            foreach (string file in Directory.GetFiles(target_dir))
            {
                File.Delete(file);
            }

            foreach (string subDir in Directory.GetDirectories(target_dir))
            {
                ForceDeleteFilesAndFoldersRecursively(subDir);
            }

            Thread.Sleep(1);
            Directory.Delete(target_dir);
        }
        public Process Run(string fileName)
        {
            if (Directory.Exists(@"Builder/ReCTClasses"))
            {
                ForceDeleteFilesAndFoldersRecursively(@"Builder/ReCTClasses");
                Directory.CreateDirectory(@"Builder/ReCTClasses");
            }

            if (!Directory.Exists(@"Builder/ReCTClasses"))
                Directory.CreateDirectory(@"Builder/ReCTClasses");

            using (StreamWriter sw = new StreamWriter(new FileStream($"Builder/ReCTClasses/{Path.GetFileNameWithoutExtension(fileName)}.rcproj", FileMode.Create)))
            {
                sw.Write("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp3.1</TargetFramework></PropertyGroup></Project>");
                sw.Flush();
                sw.Close();
            }
            File.Copy(fileName, $"Builder/ReCTClasses/{Path.GetFileName(fileName)}");

            string strCmdText;
            strCmdText = $"/K cd \"{Path.GetFullPath($"Builder/ReCTClasses")}\" & dotnet run";
            var process = Process.Start("CMD.exe", strCmdText);

            return process;
        }
    }
}
