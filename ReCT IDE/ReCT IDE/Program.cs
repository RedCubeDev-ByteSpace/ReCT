using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReCT_IDE
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (args.Length > 0)
                    Form1.fileToOpen = args[0];
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "/logs/");
                File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/logs/crash.log", ex.Message);
                Crash crash = new Crash();
                crash.ShowDialog();
                
            }

        }
    }
}
