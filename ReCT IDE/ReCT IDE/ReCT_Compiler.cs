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
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace ReCT_IDE
{
    public class ReCT_Compiler
    {
        public string Variables = "";
        public string Functions = "";
        public VariableSymbol[] variables;
        public FunctionSymbol[] functions;
        public Diagnostic[] errors = new Diagnostic[0];

        public void Check(string code, Form1 form, string inPath)
        {
            form.startAllowed(false);
            var syntaxTree = SyntaxTree.Parse(code);

            if (code.Contains("#attach"))
            {
                var lookingforfile = "";
                try
                {
                    List<string> neededFiles = new List<string>();
                    List<string> neededCode = new List<string>();
                    var matches = Regex.Matches(code, @"(?<=#attach\(\" + "\"" + @")(.*)(?=\" + "\"" + @"\))");

                    for (int i = 0; i < matches.Count; i++)
                    {
                        neededFiles.Add(matches[i].Value);
                    }

                    foreach (string p in neededFiles)
                    {
                        var lp = p;
                        if (!p.Contains(":"))
                        {
                            lp = Path.GetDirectoryName(inPath) + "\\" + p;
                        }

                        lookingforfile = lp;

                        using (StreamReader sr = new StreamReader(new FileStream(lp, FileMode.Open)))
                        {
                            neededCode.Add(sr.ReadToEnd());
                            sr.Close();
                        }
                    }

                    for (int i = 0; i < neededFiles.Count; i++)
                    {
                        code = code.Replace($"#attach(\"{neededFiles[i]}\")", neededCode[i]);
                    }

                    syntaxTree = SyntaxTree.Parse(code);
                }
                catch
                {
                    return;
                }
            }

            var compilation = Compilation.Create(syntaxTree);


            Variables = "";
            Functions = "";

            var vars = compilation.Variables.ToArray();
            variables = vars;
            foreach(VariableSymbol vs in vars)
            {
                Variables += "\\b" + vs.Name + "\\b" + "|";
            }
            if(Variables != "")
            {
                Variables = Variables.Substring(0, Variables.Length - 1);
                Variables = "(" + Variables + ")";
            }
            var fns = compilation.Functions.ToArray();
            functions = fns;
            foreach (FunctionSymbol fs in fns)
            {
                Functions += "\\b" + fs.Name + "\\b" + "|";
            }
            if (Functions != "")
            {
                Functions = Functions.Substring(0, Functions.Length - 1);
                Functions = "(" + Functions + ")";
            }
            form.startAllowed(true);
        }
        public static bool CompileRCTBC(string fileOut, string inPath, Error errorBox)
        {
            string code = "";
            using (StreamReader sr = new StreamReader(new FileStream(inPath, FileMode.Open)))
            {
                code = sr.ReadToEnd();
                sr.Close();
            }

            var syntaxTree = SyntaxTree.Parse(code);

            if (code.Contains("#attach"))
            {
                var lookingforfile = "";
                try
                {
                    while (true)
                    {
                        if (!code.Contains("#attach"))
                            break;

                        List<string> neededFiles = new List<string>();
                        List<string> neededCode = new List<string>();
                        var matches = Regex.Matches(code, @"(?<=#attach\(\" + "\"" + @")(.*)(?=\" + "\"" + @"\))");

                        for (int i = 0; i < matches.Count; i++)
                        {
                            neededFiles.Add(matches[i].Value);
                        }

                        foreach (string p in neededFiles)
                        {
                            var lp = p;
                            if (!p.Contains(":"))
                            {
                                lp = Path.GetDirectoryName(inPath) + "\\" + p;
                            }

                            lookingforfile = lp;

                            using (StreamReader sr = new StreamReader(new FileStream(lp, FileMode.Open)))
                            {
                                neededCode.Add(sr.ReadToEnd());
                                sr.Close();
                            }
                        }

                        for (int i = 0; i < neededFiles.Count; i++)
                        {
                            code = code.Replace($"#attach(\"{neededFiles[i]}\")", neededCode[i]);
                        }
                    }
                }
                catch
                {
                    errorBox.Show();
                    errorBox.errorBox.Clear();
                    errorBox.errorBox.Text = $"[L: ?, C: ?] Could not find attachment file '{lookingforfile}'!";
                    return false;
                }

                syntaxTree = SyntaxTree.Parse(code);
            }

            var sErrors = syntaxTree.Diagnostics;

            if (sErrors.Any())
            {
                Console.WriteLine("oof");
                errorBox.Show();
                errorBox.errorBox.Clear();
                foreach (Diagnostic d in sErrors)
                {
                    if (d.Location.Text != null)
                    {
                        errorBox.errorBox.Text += $"[L: {d.Location.StartLine}, C: {d.Location.StartCharacter}] {d.Message}\n";
                    }
                    else
                        errorBox.errorBox.Text += $"[L: ?, C: ?] {d.Message}\n";
                }
                errorBox.version.Text = ReCT.info.Version;
                return false;
            }

            ImmutableArray<Diagnostic> errors = ImmutableArray<Diagnostic>.Empty;

            try
            {
                var compilation = Compilation.Create(syntaxTree);
                errors = compilation.Emit(Path.GetFileNameWithoutExtension(fileOut), new string[] { @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Net.Sockets.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.IO.FileSystem.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Console.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Threading.Thread.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Threading.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll", @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.Extensions.dll" }, Path.GetDirectoryName(fileOut) + "\\" + Path.GetFileNameWithoutExtension(fileOut) + ".dll");

                Console.WriteLine(Path.GetDirectoryName(fileOut) + "\\" + Path.GetFileNameWithoutExtension(fileOut) + ".dll");

                if (errors.Any())
                {
                    errorBox.Show();
                    errorBox.errorBox.Clear();
                    foreach (Diagnostic d in errors)
                    {
                        if (d.Location.Text != null)
                        {
                            errorBox.errorBox.Text += $"[L: {d.Location.StartLine}, C: {d.Location.StartCharacter}] {d.Message}\n";
                        }
                        else
                            errorBox.errorBox.Text += $"[L: ?, C: ?] {d.Message}\n";
                    }
                    errorBox.version.Text = ReCT.info.Version;
                    return false;
                }

                //generate runtimeconfig
                using (StreamWriter sw = new StreamWriter(new FileStream(Path.GetDirectoryName(fileOut) + "\\" + Path.GetFileNameWithoutExtension(fileOut) + ".runtimeconfig.json", FileMode.Create)))
                {
                    sw.Write("{\"runtimeOptions\": {\"tfm\": \"netcoreapp3.1\",\"framework\": {\"name\": \"Microsoft.NETCore.App\",\"version\": \"3.1.0\"}}}");
                }
                using (StreamWriter sw = new StreamWriter(new FileStream(fileOut, FileMode.Create)))
                {
                    sw.Write($"dotnet exec \"{Path.GetFileNameWithoutExtension(fileOut)}.dll\"");
                }
            }
            catch (Exception e)
            {
                errorBox.Show();
                errorBox.errorBox.Clear();

                if (errors.Any())
                {
                    errorBox.Show();
                    errorBox.errorBox.Clear();
                    foreach (Diagnostic d in errors)
                    {
                        if (d.Location.Text != null)
                        {
                            errorBox.errorBox.Text += $"[L: {d.Location.StartLine}, C: {d.Location.StartCharacter}] {d.Message}\n";
                        }
                        else
                            errorBox.errorBox.Text += $"[L: ?, C: ?] {d.Message}\n";
                    }
                    errorBox.version.Text = ReCT.info.Version;
                    return false;
                }
                else
                {
                    errorBox.errorBox.Text = "THIS ERROR MIGHT BE INTERNAL! Please try again in a few seconds. (ReCT is unstable sometimes so you might have to try multiple times) \n" + errorBox.errorBox.Text;
                    errorBox.errorBox.Text += e.Source + ": " + e.Message + "\n" + e.StackTrace;
                    return false;
                }
            }
            return true;
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
