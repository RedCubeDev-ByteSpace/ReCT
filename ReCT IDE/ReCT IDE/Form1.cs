using FastColoredTextBoxNS;
using ReCT.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReCT_IDE
{
    public partial class Form1 : Form
    {
        public string openFile = "";
        public bool fileChanged = false;
        public ReCT_Compiler rectComp = new ReCT_Compiler();
        public Error errorBox;
        public Process running;
        string[] standardAC;

        public Image[] icons = new Image[7];

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        string standardMsg = "//ReCT IDE ";

        public Form1()
        {
            InitializeComponent();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Menu.Renderer = new MenuRenderer();
            standardMsg += ReCT.info.Version;
            errorBox = new Error();
            SetCodeBoxColors();
            fileChanged = false;
            updateWindowTitle();

            icons[0] = Play.BackgroundImage;
            icons[1] = Stop.BackgroundImage;
            icons[2] = Build.BackgroundImage;

            icons[3] = Image.FromFile("res/playIconHL.png");
            icons[4] = Image.FromFile("res/literally_just_a_fukin_SquareIconHL.png");
            icons[5] = Image.FromFile("res/gearIconHL.png");

            icons[6] = Image.FromFile("res/playIconLoad.png");

            standardAC = ReCTAutoComplete.Items;
        }

        public void startAllowed(bool allowed)
        {
            if(!allowed)
                Play.BackgroundImage = icons[6];
            else
                Play.BackgroundImage = icons[0];
        }

        public void changeIcon(PictureBox box, int id, bool mode)
        {
            box.BackgroundImage = icons[id + (mode ? 3 : 0)];
        }

        private class MenuRenderer : ToolStripProfessionalRenderer
        {
            public MenuRenderer() : base(new MenuColors()) { }
        }

        #region CodeBoxColors

        public void SetCodeBoxColors()
        {
            CodeBox.CaretColor = Color.White;
            CodeBox.LineNumberColor = Color.White;
            CodeBox.PaddingBackColor = Color.FromArgb(255, 25, 25, 25);
            CodeBox.IndentBackColor  = Color.FromArgb(255, 25, 25, 25);
            CodeBox.CurrentLineColor = Color.FromArgb(255, 41, 41, 41);
            CodeBox.ServiceLinesColor = Color.FromArgb(255, 25, 25, 25);
            CodeBox.SelectionColor = Color.Red;
            CodeBox.Font = new Font("Liberation Mono", 20);
            CodeBox.Text = standardMsg;
            CodeBox.AutoCompleteBrackets = true;
            CodeBox.AutoIndent = true;
            CodeBox.LineNumberStartValue = 0;
        }

        private class MenuColors : ProfessionalColorTable
        {
            public MenuColors()
            {
                base.UseSystemColors = false;
            }

            public override Color MenuItemSelected => Color.FromArgb(200, 145, 7, 7);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(100, 186, 66, 60);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(100, 120, 54, 50);
            public override Color MenuItemBorder => Color.FromArgb(255, 26, 26, 26);
            public override Color MenuItemPressedGradientBegin => Color.Transparent;
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(200, 120, 54, 50);
            public override Color MenuBorder => Color.FromArgb(255, 26, 26, 26);
            public override Color ToolStripBorder => Color.FromArgb(255, 26, 26, 26);
            public override Color ToolStripPanelGradientBegin => Color.FromArgb(255, 26, 26, 26);
        }
        #endregion

        #region Highlighting

        Style StringStyle = new TextStyle(new SolidBrush(Color.FromArgb(92, 227, 61)), null, FontStyle.Regular);
        Style VarStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 157, 227)), null, FontStyle.Bold);
        Style StatementStyle = new TextStyle(new SolidBrush(Color.FromArgb(227, 85, 75)), null, FontStyle.Bold);
        Style AttachStyle = new TextStyle(new SolidBrush(Color.FromArgb(232, 128, 121)), null, FontStyle.Regular);
        Style TypeStyle = new TextStyle(new SolidBrush(Color.FromArgb(24, 115, 163)), null, FontStyle.Regular);
        Style NumberStyle = new TextStyle(new SolidBrush(Color.FromArgb(9, 170, 179)), null, FontStyle.Regular);
        Style SystemFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(255, 131, 7)), null, FontStyle.Regular);
        Style UserFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(25, 189, 93)), null, FontStyle.Regular);
        Style VariableStyle = new TextStyle(new SolidBrush(Color.FromArgb(255, 212, 125)), null, FontStyle.Regular);
        Style TypeFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(159, 212, 85)), null, FontStyle.Regular);
        Style CommentStyle = new TextStyle(new SolidBrush(Color.FromArgb(100, 100, 100)), null, FontStyle.Regular);
        Style WhiteStyle = new TextStyle(Brushes.White, null, FontStyle.Regular);

        public void ReloadHightlighting(TextChangedEventArgs e)
        {
            e.ChangedRange.ClearFoldingMarkers();

            //set folding markers [DarkMode]
            e.ChangedRange.SetFoldingMarkers("{", "}");

            //Dev highlighting [lol]
            //e.ChangedRange.SetStyle(RedStyleDM, @"(ProfessorDJ|Realmy|RedCube)");

            //clear style of range [DarkMode]
            e.ChangedRange.ClearStyle(CommentStyle);

            //quotes
            e.ChangedRange.SetStyle(StringStyle, "\\\"(.*?)\\\"", RegexOptions.Singleline);

            //comment highlighting [DarkMode]
            e.ChangedRange.SetStyle(CommentStyle, @"//.*$", RegexOptions.Multiline);

            e.ChangedRange.SetStyle(AttachStyle, @"(#attach\b)", RegexOptions.Singleline);

            //clear style of range [DarkMode]
            e.ChangedRange.ClearStyle(SystemFunctionStyle);

            //system function highlighting
            e.ChangedRange.SetStyle(SystemFunctionStyle, @"(\bFloor\b|\bCeil\b|\bInputAction\b|\bSetConsoleForeground\b|\bSetConsoleBackground\b|\bSetCursorVisible\b|\bThread\b|\bGetCursorVisible\b|\bPrint\b|\bInputKey\b|\bInput\b|\bRandom\b|\bVersion\b|\bClear\b|\bSetCursor\b|\bGetSizeX\b|\bGetSizeY\b|\bSetSize\b|\bWrite\b|\bSleep\b)");

            //types
            e.ChangedRange.SetStyle(TypeStyle, @"(\b\?\b|\bany\b|\bbool\b|\bint\b|\bstring\b|\bvoid\b|\bfloat\b|\bthread\b|\banyArr\b|\bboolArr\b|\bintArr\b|\bstringArr\b|\bfloatArr\b|\bthreadArr\b)");

            //function highlighting [DarkMode]
            e.ChangedRange.SetStyle(VarStyle, @"(\bvar\b|\bset\b|\bif\b|\belse\b|\bfunction\b|\btrue\b|\bfalse\b|\bmake\b|\barray\b)", RegexOptions.Singleline);

            //variables
            e.ChangedRange.SetStyle(VariableStyle, @"(\w+(?=\s+<-))");
            e.ChangedRange.SetStyle(VariableStyle, @"(\w+(?=\s+->))");
            e.ChangedRange.SetStyle(VariableStyle, rectComp.Variables);

            //functions
            e.ChangedRange.SetStyle(UserFunctionStyle, @"(?<=\bfunction\s)(\w+)");
            e.ChangedRange.SetStyle(UserFunctionStyle, rectComp.Functions);

            //type functions
            e.ChangedRange.SetStyle(TypeFunctionStyle, @"(?<=\>>\s)(\w+)");

            //statements highlighting
            e.ChangedRange.SetStyle(StatementStyle, @"(\bbreak\b|\bcontinue\b|\bfor\b|\breturn\b|\bto\b|\bwhile\b|\bdo\b|\bend\b|\bfrom\b)", RegexOptions.Singleline);

            //numbers
            e.ChangedRange.SetStyle(NumberStyle, @"(\b\d+\b)", RegexOptions.Multiline);

            //set standard text color
            e.ChangedRange.SetStyle(WhiteStyle, @".*", RegexOptions.Multiline);
        }

        Style ErrorMarker = new TextStyle(Brushes.White, Brushes.Red, FontStyle.Regular);
        void markError(TextChangedEventArgs e)
        {
            e.ChangedRange.SetStyle(ErrorMarker, ".*", RegexOptions.Multiline);
        }

        #endregion

        private void New_Click(object sender, EventArgs e)
        {
            if (!fileChanged)
            {
                CodeBox.Text = standardMsg;
                CodeBox.ClearUndo();
                openFile = "";
            }
            else
            {
                var result = MessageBox.Show("You have unsaved changes!\nAre you sure you want to create a new File?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    CodeBox.Text = standardMsg;
                    CodeBox.ClearUndo();
                    fileChanged = false;
                    openFile = "";
                }
            }
            updateWindowTitle();
        }

        private void Open_Click(object sender, EventArgs e)
        {
            if(fileChanged)
            {
                var result = MessageBox.Show("You have unsaved changes!\nAre you sure you want to open a File?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            openFileDialog1.Filter = "ReCT code files (*.rct)|*.rct|All files (*.*)|*.*";
            var res = openFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            using (StreamReader sr = new StreamReader(new FileStream(openFileDialog1.FileName, FileMode.Open)))
            {
                CodeBox.Text = sr.ReadToEnd();
                CodeBox.ClearUndo();
                sr.Close();
            }

            openFile = openFileDialog1.FileName;
            fileChanged = false;
            updateWindowTitle();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if(openFile == "")
            {
                SaveAs_Click(sender, e);
                return;
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(openFile, FileMode.Create)))
            {
                sw.Write(CodeBox.Text);
                sw.Close();
            }

            fileChanged = false;

            updateWindowTitle();
        }

        private void SaveAs_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "ReCT code files (*.rct)|*.rct|All files (*.*)|*.*";
            var res = saveFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            using (StreamWriter sw = new StreamWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create)))
            {
                sw.Write(CodeBox.Text);
                sw.Close();
            }

            openFile = saveFileDialog1.FileName;
            fileChanged = false;
            updateWindowTitle();
        }

        void updateWindowTitle()
        {
            var title = "ReCT IDE";

            title += " - ";

            if (fileChanged) title += "*";

            title += openFile == "" ? "Untitled" : Path.GetFileName(openFile);

            Text = title;
        }
        void edited()
        {
            if(!fileChanged)
            {
                fileChanged = true;
                updateWindowTitle();
            }
        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                rectComp.Variables = "";
                if (CodeBox.Text != "")
                {
                    rectComp.Check(CodeBox.Text, this);
                    CodeBox.ClearStylesBuffer();
                    ReloadHightlighting(new TextChangedEventArgs(CodeBox.Range));

                    List<string> ACItems = new List<string>();

                    foreach (string s in standardAC)
                    {
                        ACItems.Add(s);
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in rectComp.functions)
                    {
                        ACItems.Add(f.Name);
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.VariableSymbol v in rectComp.variables)
                    {
                        ACItems.Add(v.Name);
                    }

                    ReCTAutoComplete.Items = ACItems.ToArray();
                }
            }
            catch(Exception ee)
            {
                //Console.WriteLine(ee);
            }
        }

        private void autoFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CodeBox.DoAutoIndent();
        }

        private void Build_Click(object sender, EventArgs e)
        {
            Typechecker.Enabled = false;
            errorBox.Hide();
            saveFileDialog1.Filter = "Launcher (*.cmd)|*.cmd|All files (*.*)|*.*";
            var res = saveFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            if (fileChanged)
                Save_Click(this, new EventArgs());

            rectComp.CompileRCTBC (saveFileDialog1.FileName, openFile, errorBox);

            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", saveFileDialog1.FileName));
            Typechecker.Enabled = true;
        }

        private void CodeBox_Chnaged(object sender, TextChangedEventArgs e)
        {
            ReloadHightlighting(e);
            edited();
        }

        private void Play_Click(object sender, EventArgs e)
        {
            errorBox.Hide();

            if (fileChanged)
                Save_Click(this, new EventArgs());

            //clear Builder dir

            if (Directory.Exists("Builder"))
                ReCT_Compiler.ForceDeleteFilesAndFoldersRecursively("Builder");
            if (!Directory.Exists("Builder"))
                Directory.CreateDirectory("Builder");

            if (!rectComp.CompileRCTBC("Builder/" + Path.GetFileNameWithoutExtension(openFile) + ".cmd", openFile, errorBox)) return;

            string strCmdText = $"/K cd \"{Path.GetFullPath($"Builder")}\" & cls & \"{Path.GetFileNameWithoutExtension(openFile)}.cmd\"";
            running = Process.Start("CMD.exe", strCmdText);
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            try
            {
                KillProcessAndChildren(running.Id);
            } catch {}
        }

        private static void KillProcessAndChildren(int pid)
        {
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private void Play_MouseEnter(object sender, EventArgs e)
        {
            changeIcon(Play, 0, true);
        }

        private void Play_MouseLeave(object sender, EventArgs e)
        {
            changeIcon(Play, 0, false);
        }

        private void Stop_MouseEnter(object sender, EventArgs e)
        {
            changeIcon(Stop, 1, true);
        }

        private void Stop_MouseLeave(object sender, EventArgs e)
        {
            changeIcon(Stop, 1, false);
        }

        private void Build_MouseEnter(object sender, EventArgs e)
        {
            changeIcon(Build, 2, true);
        }

        private void Build_MouseLeave(object sender, EventArgs e)
        {
            changeIcon(Build, 2, false);
        }

        private void CodeBox_Load(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void buildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Play_Click(sender, e);
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Build_Click(sender, e);
        }
    }
}
