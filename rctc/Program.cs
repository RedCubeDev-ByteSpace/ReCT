using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReCT.CodeAnalysis;
using ReCT.CodeAnalysis.Syntax;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Binding;
using ReCT.IO;
using Mono.Options;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;

namespace ReCT
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string outputPath = default;
            string moduleName = default;

            bool helpRequested = false;
            bool useFlags = false;

			bool dryRun = false;
			bool jsonError = false;

            List<string> referencePaths = new List<string>();
            List<string> sourcePaths = new List<string>();

            List<string> filesToCopy = new List<string>();
            List<string> foldersToCopy = new List<string>();

            OptionSet options = new OptionSet
            {
                "usage: rctc <source-paths> [options]",
                { "r=", "The {path} of an assembly to reference", v => referencePaths.Add(v) },
                { "s|stdasm", "Will automatically reference all Standard Assemblies", v => referenceStandardAssemblies(ref referencePaths) },
                { "o=", "The output {path} of the assembly to create", v => outputPath = v },
                { "m=", "The {name} of the module", v => moduleName = v },
                { "f", "Use IDE compiler Flags", v => useFlags = true },
                { "d", "Dry run and return JSON compilation data", v => dryRun = true },
                { "je", "Return JSON error data", v => jsonError = true },
                { "v", "Display version of rctc", v => { Console.WriteLine(ReCT.info.Version); Environment.Exit(0); } },
                { "?|h|help", "Prints help", v => helpRequested = true },
                { "<>", v => sourcePaths.Add(v) }
            };

			if (args.Length == 0 || args[0] != "run")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-------------------------------");
                Console.WriteLine("ReCT Standalone Compiler " + info.Version);
                Console.WriteLine("-------------------------------\n");
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (args.Length != 0 && args[0] == "create") {
                projectActions(args);
                return;
            }

            if (args.Length != 0 && args[0] == "run") {
                projectRun();
                return;
            }

            options.Parse(args);

            if (helpRequested)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (sourcePaths.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: need at least one source file");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            else
                Console.WriteLine("Source: " + sourcePaths[0]);

            if (outputPath == null)
                outputPath = Path.ChangeExtension(sourcePaths[0], "dll");

            if (moduleName == null)
                moduleName = Path.GetFileNameWithoutExtension(outputPath);

            Console.WriteLine("Output: " + outputPath);

            //making sure the relative paths still work after directory change
            for (int i = 0; i < sourcePaths.Count; i++)
                sourcePaths[i] = Path.GetFullPath(sourcePaths[i]);

            outputPath = Path.GetFullPath(outputPath);
            
            //change working directory to access compiler files
            var callcwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

            SyntaxTree[] syntaxTrees = new SyntaxTree[sourcePaths.Count];
			List<ReCTAttachment> attachments = new List<ReCTAttachment>();
			string code = "";
			string mainCode = "";

            for (int pathIndex = 0; pathIndex < sourcePaths.Count; pathIndex++)
            {
                if (!File.Exists(sourcePaths[pathIndex]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: file '{sourcePaths[pathIndex]}' doesn't exist");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                code = File.ReadAllText(sourcePaths[pathIndex]);
				mainCode = code;

                if (useFlags)
                {
                    Console.WriteLine($"Evaluating Flags for file '{Path.GetFileName(sourcePaths[pathIndex])}' ...");
                    attachments.AddRange(evaluateFlags(ref code, ref filesToCopy, ref foldersToCopy, sourcePaths[pathIndex]));
                }
                
                SyntaxTree syntaxTree = SyntaxTree.Parse(code);

                ImmutableArray<Diagnostic> parserDiagnostics = syntaxTree.Diagnostics;

                if (parserDiagnostics.Length != 0)
                {
					if (jsonError)
						outputJSONDiagnostics(parserDiagnostics, mainCode, attachments.ToArray());
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Error.WriteDiagnostics(parserDiagnostics, attachments.ToArray());
						Console.ForegroundColor = ConsoleColor.White;
					}
                    return;
                }

                syntaxTrees[pathIndex] = syntaxTree;
            }

            for (int pathIndex = 0; pathIndex < referencePaths.Count; pathIndex++)
                if (!File.Exists(referencePaths[pathIndex]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: file '{referencePaths[pathIndex]}' doesn't exist");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

            Console.WriteLine("Compiling...");
            
            Compilation compilation = Compilation.Create(syntaxTrees.ToArray());

			if (dryRun)
			{
				compilation.PrepareProgram();
				ReturnCompilationData(compilation);
				return;
			}

            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(moduleName, referencePaths.ToArray(), outputPath);

            if (diagnostics.Length != 0)
            {
				if (jsonError)
					outputJSONDiagnostics(diagnostics, mainCode, attachments.ToArray());
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Error.WriteDiagnostics(diagnostics, attachments.ToArray());
					Console.ForegroundColor = ConsoleColor.White;
				}
                return;
            }
            
            //create runtime config
            Console.WriteLine("Creating runtimeconfig.json ...");
            File.WriteAllText(Path.ChangeExtension(outputPath, "runtimeconfig.json"), "{\"runtimeOptions\": {\"tfm\": \"netcoreapp3.1\",\"framework\": {\"name\": \"Microsoft.NETCore.App\",\"version\": \"3.1.0\"}}}");
            
            //copy needed packages
            Console.WriteLine("Copying needed Packages...");
            foreach (CodeAnalysis.Package.Package p in compilation.Packages)
            {
                if (p.fullName.EndsWith(".dll"))
                {
                    File.Copy(p.fullName, Path.GetDirectoryName(outputPath) + "/" + Path.GetFileName(p.fullName), true);
                    continue;
                }

                File.Copy(p.fullName, Path.GetDirectoryName(outputPath) + "/" + p.name + "lib.dll", true);

                if (p.name == "audio")
                    File.Copy("System Dotnet Assemblies/NetCoreAudio.dll", Path.GetDirectoryName(outputPath) + "/" + "NetCoreAudio.dll", true);
            }
            
            //copy files and folders
            foreach (string s in filesToCopy)
            {
                Console.WriteLine("Copying File: " + Path.GetFileName(s));
                var source = s.Replace("$CWD", callcwd);
                if (Path.IsPathRooted(source))
                    File.Copy(source, Path.GetDirectoryName(outputPath) + "/" + Path.GetFileName(s), true);
                else
                    File.Copy("Packages/" + source, Path.GetDirectoryName(outputPath) + "/" + Path.GetFileName(source), true);
            }
            foreach (string s in foldersToCopy)
            {
                Console.WriteLine("Copying Folder: " + s.Split('\\').Last().Split('/').Last());
                
                var SourcePath = s.Replace("$CWD", callcwd);
                var DestinationPath = Path.GetDirectoryName(outputPath) + "/" + s.Split('\\').Last().Split('/').Last();
                if (!Path.IsPathRooted(SourcePath)) SourcePath = "Packages/" + SourcePath;

                Directory.CreateDirectory(DestinationPath);

                foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

                //Copy all the files
                foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=> Compilation Complete!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void projectRun()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("RCTC ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(">> Project Run");
            Console.ForegroundColor = ConsoleColor.White;
            var rcp = Directory.GetFiles("./").FirstOrDefault(x => x.EndsWith(".rcp"));

            if (rcp == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Couldnt find Project File (.rcp)!");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            var rcpText = File.ReadAllText(rcp);
            var data = JsonSerializer.Deserialize<ReCTProject>(rcpText);

            if (File.Exists($"./Build/{data.Name}.dll"))
                File.Delete($"./Build/{data.Name}.dll");

            var dirpath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var exename = File.Exists(dirpath + "/rctc") ? "rctc" : "rctc.exe";

            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = dirpath + "/" + exename,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"./Classes/{data.MainClass} -s -f -o ./Build/{data.Name}.dll"
            });
            process.WaitForExit();

            if (File.Exists($"./Build/{data.Name}.dll"))
            {
                Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/Build");
                var proc = Process.Start("dotnet", $"./{data.Name}.dll");
                proc.WaitForExit();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Build has Failed!");
                Console.WriteLine(process.StandardError.ReadToEnd());
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static void projectActions(string[] args)
        {
            Console.WriteLine("Project Generator");
            args = args.Skip(1).ToArray();

            var projectPath = "./";
            var newDir = false;
            var vscodeConfig = false;
            var helpRequested = false;
            var name = "";

            OptionSet projectOptions = new OptionSet
            {
                "usage: rctc create <name> [options]",
                { "d=", "The {path} to create the Project at", v => projectPath = v },
                { "n|newdir", "Will create a new Directory for the Project", v => newDir = true },
                { "v|vscode", "Will generate .vscode folder with Run-Config", v => vscodeConfig = true },
                { "?|h|help", "Prints help", v => helpRequested = true },
                { "<>", v => name = v }
            };

            projectOptions.Parse(args);

            if (helpRequested){
                projectOptions.WriteOptionDescriptions(Console.Out); return;
            }

            if (name == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please provide a name for the Project!");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Given Directory doesnt exist!");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            if (newDir)
            {
                var dirname = Path.Combine(projectPath, name);
                var index = 0;
                while (Directory.Exists(dirname))
                    dirname = Path.Combine(projectPath, name + index++);

                Directory.CreateDirectory(Path.Combine(projectPath, dirname));
                projectPath = Path.Combine(projectPath, dirname);
            }

            if (Directory.Exists(Path.Combine(projectPath, "Classes")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Couldnt create 'Classes' folder! (already exists)");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            if (Directory.Exists(Path.Combine(projectPath, "Build")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Couldnt create 'Build' folder! (already exists)");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            Console.WriteLine("Creating 'Classes' dir...");
            Directory.CreateDirectory(Path.Combine(projectPath, "Classes"));
            Console.WriteLine("Creating 'Build' dir...");
            Directory.CreateDirectory(Path.Combine(projectPath, "Build"));
            Console.WriteLine("Creating 'Classes/main.rct' file...");
            File.WriteAllText(Path.Combine(projectPath, "Classes/main.rct"), $"// {name} - ReCT {info.Version} \npackage sys; use sys;\n\nPrint(\"Hello World!\");");
            Console.WriteLine("Creating 'build.sh' file...");
            File.WriteAllText(Path.Combine(projectPath, "build.sh"), $"rm './Build/{name}.dll'\nrctc ./Classes/main.rct -s -f -o './Build/{name}.dll'\necho '-- [ReCT Program] --'\necho ''\ndotnet './Build/{name}.dll'");
            Console.WriteLine("Creating 'build.cmd' file...");
            File.WriteAllText(Path.Combine(projectPath, "build.cmd"), $"del './Build/{name}.dll'\nrctc ./Classes/main.rct -s -f -o './Build/{name}.dll'\necho -- [ReCT Program] --\necho \ndotnet './Build/{name}.dll'");
        
            Console.WriteLine("Creating '"+name+".rct' file...");
            File.WriteAllText(Path.Combine(projectPath, $"{name}.rcp"), "{\"Name\": \""+name+"\", \"Icon\": \"\", \"MainClass\": \"main.rct\"}");

            if (vscodeConfig)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nGenerating VSCode files..");
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("Creating '.vscode' dir..."); Directory.CreateDirectory(Path.Combine(projectPath, ".vscode"));
                Console.WriteLine("Creating '.vscode/launch.json' file...");
                File.WriteAllText(Path.Combine(projectPath, ".vscode/launch.json"), "{\"version\": \"0.2.0\",\"configurations\": [{\"name\": \".NET Core Launch (console)\",\"type\": \"coreclr\",\"request\": \"launch\",\"preLaunchTask\": \"buildrect\",\"program\": \"dotnet\",\"args\": [\"${workspaceFolder}/Build/"+name+".dll\"],\"cwd\": \"${workspaceFolder}\",\"stopAtEntry\": false,\"console\": \"internalConsole\"}]}");
                Console.WriteLine("Creating '.vscode/tasks.json' file...");
                File.WriteAllText(Path.Combine(projectPath, ".vscode/tasks.json"), "{\"version\": \"2.0.0\",\"tasks\": [{\"label\": \"buildrect\",\"command\": \"rctc\",\"type\": \"shell\",\"args\": [\"./Classes/main.rct\", \"-s\", \"-f\", \"-o\", \"./Build/"+name+".dll\"],\"presentation\": {\"reveal\": \"always\"},\"group\": \"build\"}]}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=> Project Created!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void referenceStandardAssemblies(ref List<string> references)
        {
            Console.WriteLine("Referencing standard Assemblies...");
            var location = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            references.Add(location + "/System Dotnet Assemblies/System.Console.dll");
            references.Add(location + "/System Dotnet Assemblies/System.IO.FileSystem.dll");
            references.Add(location + "/System Dotnet Assemblies/System.Net.Sockets.dll");
            references.Add(location + "/System Dotnet Assemblies/System.Runtime.dll");
            references.Add(location + "/System Dotnet Assemblies/System.Runtime.Extensions.dll");
            references.Add(location + "/System Dotnet Assemblies/System.Threading.dll");
            references.Add(location + "/System Dotnet Assemblies/System.Threading.Thread.dll");
        }

        static List<ReCTAttachment> evaluateFlags(ref string code, ref List<string> filesToCopy, ref List<string> foldersToCopy, string inPath)
        {
            Console.WriteLine("Evaluating Flags...");

			List<ReCTAttachment> attachments = new List<ReCTAttachment>();

            var lookingforfile = "";
            try
            {
                while (code.Contains("#attach"))
                {
                    string neededFile = "";
                    var matches = Regex.Matches(code, @"(?<=#attach\(\" + "\"" + @")(.*)(?=\" + "\"" + @"\))");

                    neededFile = matches[0].Value;
                    lookingforfile = neededFile;

					// new attachment object
					ReCTAttachment attachment = new ReCTAttachment();

					// set name of attachment
					attachment.Name = Path.GetFileName(lookingforfile);

					// figure out starting character
					attachment.startingIndex = matches[0].Index - 9;


                    if (!Path.IsPathRooted(lookingforfile))
                        lookingforfile = Path.GetDirectoryName(inPath) + "/" + neededFile;

                    var codeFromFile = File.ReadAllText(lookingforfile);

					// set length of attatchment
					attachment.Code = codeFromFile;
					attachment.Length = codeFromFile.Length;
					attachment.Lines = codeFromFile.Split("\n").Length;

                    code = code.Replace($"#attach(\"{neededFile}\")", codeFromFile);

					// add attachment to the pile
					attachments.Add(attachment);
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[L: ?, C: ?] Could not find attachment file '{lookingforfile}'!");
                Console.ForegroundColor = ConsoleColor.White;
                return attachments;
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            if (code.Contains("#closeConsole"))
                Console.WriteLine("Flag '#closeConsole' is not supported in Standalone mode, will be ignored.");

            if (code.Contains("#noConsole"))
                Console.WriteLine("Flag '#noConsole' is not supported in Standalone mode, will be ignored.");
            
            Console.ForegroundColor = ConsoleColor.White;
            
            while (code.Contains("#copyFolder"))
            {
                var matches = Regex.Matches(code, @"(?<=#copyFolder\(\" + "\"" + @")(.*)(?=\" + "\"" + @"\))");

                for (int i = 0; i < matches.Count; i++)
                {
                    foldersToCopy.Add(matches[i].Value);
                    code = code.Replace($"#copyFolder(\"{matches[i].Value}\")", "");
                }
            }

            while (code.Contains("#copy"))
            {
                var matches = Regex.Matches(code, @"(?<=#copy\(\" + "\"" + @")(.*)(?=\" + "\"" + @"\))");

                for (int i = 0; i < matches.Count; i++)
                {
                    filesToCopy.Add(matches[i].Value);
                    code = code.Replace($"#copy(\"{matches[i].Value}\")", "");
                }
            }

			return attachments;
        }

		static void outputJSONDiagnostics(IEnumerable<Diagnostic> diagnostics, string mainCode, ReCTAttachment[] attachments)
		{
			List<ReCTError> errors = new List<ReCTError>();

			foreach (var diagnostic in diagnostics.Where(d => d.Location.Text == null))
            {
				ReCTError newError = new ReCTError()
				{
					ErrorMessage = diagnostic.Message
				};
				errors.Add(newError);
            }

            foreach (var diagnostic in diagnostics.Where(d => d.Location.Text != null)
                                                  .OrderBy(d => d.Location.FileName)
                                                  .ThenBy(d => d.Location.Span.Start)
                                                  .ThenBy(d => d.Location.Span.Length))
            {
                var text = diagnostic.Location.Text;
                var startLine = diagnostic.Location.StartLine + 1;
                var startCharacter = diagnostic.Location.StartCharacter + 1;
				var endLine = diagnostic.Location.EndLine + 1;
                var endCharacter = diagnostic.Location.EndCharacter + 1;

				var lineDifference = endLine - startLine;

                var span = diagnostic.Location.Span;
                var lineIndex = text.GetLineIndex(span.Start);
                var line = text.Lines[lineIndex];


                var attachment = "";
				ReCTAttachment attachmentObject = null;

				foreach(var att in attachments)
					if (span.Start >= att.startingIndex && span.End < att.startingIndex + att.Length)
					{
						attachment = "[File '" + att.Name + "']\n";
						startLine = att.Code.Substring(0, span.Start - att.startingIndex).Split('\n').Length;
						endLine = startLine + lineDifference;
						attachmentObject = att;
						break;
					}

				// if we arent inside of an attachment -> adjust line count
				if (attachment == "")
				{
					int goBackBy = 0;
					int attLines = 0;
					foreach(var att in attachments)
						if (att.startingIndex < span.Start)
						{
							goBackBy += att.Lines;
							attLines++;
						}

					startLine = startLine - goBackBy + attLines;
					endLine = startLine + lineDifference;
				}

				// figure out start and end index
				String code = mainCode;
				if (attachmentObject != null)
					code = attachmentObject.Code;

				int startingIndex = -1;
				int endingIndex = -1;

				int lineNum = 1;
				int charNum = 0;

				//Console.WriteLine("-[ " + diagnostic.Message + " ]--------");

				for (int i = 0; i < code.Length; i++)
				{
					charNum++;

					if (code[i] == '\n') 
					{
						// in case the end is in some unreachable location but still this line
						if (lineNum == endLine)
							endingIndex = i - 1;

						lineNum++;
						charNum = 1;
					}

					if (lineNum == startLine && charNum == startCharacter)
						startingIndex = i;

					if (lineNum == endLine && charNum == endCharacter)
						endingIndex = i;

					//Console.WriteLine("LINE: " + lineNum + "; ENDLINE: " + endLine + "; ENDCHAR: " + endCharacter + "; CHAR: " + i);
				}

				if (startingIndex != -1 && endingIndex == -1)
					endingIndex = startingIndex + 1;

				ReCTError newError = new ReCTError()
				{
					ErrorMessage = diagnostic.Message,
					Attachment   = attachment,

					startLine    = startLine,
					startChar    = startCharacter,

					endLine      = endLine,
					endChar      = endCharacter,

					startIndex   = startingIndex,
					endIndex     = endingIndex

				};
				errors.Add(newError);
            }

			ReCTErrorData errorData = new ReCTErrorData();
			errorData.Errors = errors.ToArray();

			// jsonizing
			Console.WriteLine("\nERRDATA");
			Console.WriteLine(JsonSerializer.Serialize(errorData));
		}

		static void ReturnCompilationData(Compilation compilation)
		{
			var data = new ReturnCompilationData();

			List<ReCTGlobal>   globals   = new List<ReCTGlobal>();
			List<ReCTVariable> variables = new List<ReCTVariable>();
			List<ReCTFunction> functions = new List<ReCTFunction>();
			List<ReCTClass>    classes   = new List<ReCTClass>();
			List<ReCTEnum>     enums     = new List<ReCTEnum>();
			List<ReCTPackage>  packages  = new List<ReCTPackage>();

			// globals + main variables
			foreach(var variable in compilation.Variables)
				CollectVariableData(ref variables, ref globals, variable);

			// function data
			foreach(var fnc in compilation.Functions)
				CollectFunctionData(ref functions, ref globals, fnc);

			// classes
			foreach(var cls in compilation.Classes)
				CollectClassData(ref classes, ref globals, cls);

			// enums
			foreach(var enm in compilation.Enums)
				CollectEnumData(ref enums, ref globals, enm);

			// packages
			foreach(var pck in compilation.Packages)
				CollectPackageData(ref packages, pck, compilation);

			// finalizing
			data.Globals = globals.ToArray();
			data.Variable = variables.ToArray();
			data.Functions = functions.ToArray();
			data.Classes = classes.ToArray();
			data.Enums = enums.ToArray();
			data.Packages = packages.ToArray();

			data.UsingPackageNamespaces = compilation.UsingPackages.ToArray();


			// jsonizing
			Console.WriteLine("\nDATA");
			Console.WriteLine(JsonSerializer.Serialize(data));
		}

		private static void CollectPackageData(ref List<ReCTPackage> packages, CodeAnalysis.Package.Package pck, Compilation cmp)
		{
			var package = new ReCTPackage();
			package.Name = pck.name;
			package.FullName = pck.fullName;

			List<string> aliases = new List<string>();
			
			foreach(KeyValuePair<string, string> alias in cmp.Aliases)
			if (alias.Value == pck.name)
				aliases.Add(alias.Key);

			package.Aliases = aliases.ToArray();

			// package contents
			List<ReCTGlobal>   globals   = new List<ReCTGlobal>();
			List<ReCTFunction> functions = new List<ReCTFunction>();
			List<ReCTClass>    classes   = new List<ReCTClass>();

			// functions
			foreach(var fnc in pck.scope.GetDeclaredFunctions())
				CollectFunctionData(ref functions, ref globals, fnc);

			// classes
			foreach(var cls in pck.scope.GetDeclaredClasses())
				CollectClassData(ref classes, ref globals, cls);

			package.Functions = functions.ToArray();
			package.Classes = classes.ToArray();

			packages.Add(package);
		}

		static void CollectFunctionData(ref List<ReCTFunction> functions, ref List<ReCTGlobal> globals, FunctionSymbol fnc)
		{
			var function = new ReCTFunction();
			function.Name = fnc.Name;
			function.Returntype = fnc.Type.Name;

			List<ReCTVariable> variables = new List<ReCTVariable>();
			List<ReCTVariable> parameters = new List<ReCTVariable>();

			if (fnc.scope != null)
			{
				foreach(var vr in fnc.scope.GetDeclaredVariables())
					CollectVariableData(ref variables, ref globals, vr);
			}

			foreach(var param in fnc.Parameters)
				parameters.Add(new ReCTVariable(){ Name = param.Name, Datatype = param.Type.Name});


			variables.AddRange(parameters);
			function.Variables = variables.ToArray();
			function.Parameters = parameters.ToArray();
			functions.Add(function);
		}

		static void CollectClassData(ref List<ReCTClass> classes, ref List<ReCTGlobal> globals, ClassSymbol cls)
		{
			var _class = new ReCTClass();
			_class.Name = cls.Name;

			List<ReCTGlobal> properties = new List<ReCTGlobal>();
			List<ReCTFunction> functions = new List<ReCTFunction>();

			foreach(var vr in cls.Scope.GetDeclaredVariables())
				CollectGlobalData(ref properties, vr);

			foreach(var fnc in cls.Scope.GetDeclaredFunctions())
				CollectFunctionData(ref functions, ref properties, fnc);

			_class.Properties = properties.ToArray();
			_class.Functions = functions.ToArray();

			classes.Add(_class);
		}

		static void CollectEnumData(ref List<ReCTEnum> enums, ref List<ReCTGlobal> globals, EnumSymbol enm)
		{
			var _enum = new ReCTEnum();
			_enum.Name = enm.Name;

			List<ReCTGlobal> properties = new List<ReCTGlobal>();

			foreach(KeyValuePair<string, int> val in enm.Values)
				properties.Add(new ReCTGlobal(){ Name = val.Key, Datatype = "int" });
				

			_enum.Properties = properties.ToArray();

			enums.Add(_enum);
		}

		static void CollectVariableData(ref List<ReCTVariable> variables, ref List<ReCTGlobal> globals, VariableSymbol variable, bool ignoreGlobals = false)
		{
			if (!variable.IsGlobal)
				variables.Add(new ReCTVariable { Name = variable.Name, Datatype = variable.Type.Name});
			else if (!ignoreGlobals)
				globals.Add(new ReCTGlobal { Name = variable.Name, Datatype = variable.Type.Name });
		}

		static void CollectGlobalData(ref List<ReCTGlobal> globals, VariableSymbol variable)
		{
			if (variable.IsGlobal)
				globals.Add(new ReCTGlobal { Name = variable.Name, Datatype = variable.Type.Name });
		}
    }

    class ReCTProject
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string MainClass { get; set; }
    }

	[System.Serializable]
	class ReCTFunction
	{
		public string Name { get; set; }
		public string Returntype { get; set; }
		public ReCTVariable[] Variables{ get; set; }
		public ReCTVariable[] Parameters{ get; set; }
	}

	[System.Serializable]
	class ReCTClass
	{
		public string Name { get; set; }
		public ReCTGlobal[] Properties { get; set; }
		public ReCTFunction[] Functions { get; set; }
	}

	[System.Serializable]
	class ReCTEnum
	{
		public string Name { get; set; }
		public ReCTGlobal[] Properties { get; set; }
	}

	[System.Serializable]
	class ReCTPackage
	{
		public string Name { get; set; }
		public string FullName { get; set; }
		public string[] Aliases { get; set; }
		public ReCTClass[] Classes { get; set; }
		public ReCTFunction[] Functions { get; set; }
	}

	[System.Serializable]
	class ReCTVariable
	{
		public string Name { get; set; }
		public string Datatype { get; set; }
	}

	[System.Serializable]
	class ReCTGlobal
	{
		public string Name { get; set; }
		public string Datatype { get; set; }
	}

	[System.Serializable]
	class ReturnCompilationData
	{

		public ReCTGlobal[] Globals { get; set; }
		public ReCTVariable[] Variable { get; set; }
		public ReCTFunction[] Functions { get; set; }
		public ReCTClass[] Classes { get; set; }
		public ReCTEnum[] Enums { get; set; }
		public ReCTPackage[] Packages { get; set; }

		public string[] UsingPackageNamespaces {get; set; }
	}

	[System.Serializable]
	class ReCTError
	{
		public string Attachment {get; set;}
		public string ErrorMessage {get; set;}
		public int startLine {get; set;}
		public int startChar {get; set;}
		public int endLine {get; set;}
		public int endChar {get; set;}
		public int startIndex {get; set;}
		public int endIndex {get; set;}
	}

	[System.Serializable]
	class ReCTErrorData
	{
		public ReCTError[] Errors {get; set;}
	}
}
