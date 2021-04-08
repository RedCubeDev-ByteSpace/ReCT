using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            var name = getDLLname();
            var settings = getOptions();
            //var rtcg = Path.GetFileNameWithoutExtension(name) + ".runtimeconfig.json";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = !settings[1];
            startInfo.UseShellExecute = false;        
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;          
            startInfo.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            if (!settings[0])
            {
                startInfo.FileName = "dotnet.exe";
                startInfo.Arguments = "exec \"" + Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\" + name + "\"";
            }
            else
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/K @dotnet exec \"" + Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\" + name + "\"";
            }

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        static string getDLLname()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()[0];
        }

        private static bool[] getOptions()
        {
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()[1];
            Stream theResource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

            byte[] bytes = new byte[theResource.Length];
            theResource.Read(bytes, 0, (int)theResource.Length);
            var optionFile = System.Text.Encoding.Default.GetString(bytes);
            string[] options = optionFile.Split('\n');
            bool[] values = new bool[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                values[i] = bool.Parse(options[i].Split(':')[1]);
                Console.WriteLine(values[i]);
            }

            return values;
        }
    }
}