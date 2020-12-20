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
using DiscordRPC;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ReCT_IDE
{
    public partial class Form1 : Form
    {
        public string openFile = "";
        public bool fileChanged = false;
        public ReCT_Compiler rectCompCheck = new ReCT_Compiler();
        public ReCT_Compiler rectCompBuild = new ReCT_Compiler();
        public Error errorBox;
        public Process running;
        string[] standardAC;
        BoltUpdater boltUpdater;

        public FastColoredTextBox stdBox;

        public static string fileToOpen = "";

        Discord dc;
        RichPresence presence;

        Settings settings;

        bool tabSwitch = false;

        public Button TabPrefab;

        public Image[] icons = new Image[7];

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int cmdShow);


        string standardMsg = "//ReCT Compiler and IDE ";

        List<Tab> tabs = new List<Tab>();
        int currentTab = 0;

        public Form1()
        {
            //make sure program path is set correctly
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            //set file associations and icons
            FileAssociations.EnsureAssociationsSet();

            Thread t = new Thread(new ThreadStart(SplashScreen));
            t.Start();

            boltUpdater = new BoltUpdater();
            if (boltUpdater.isUpdateAvailable(ReCT.info.Version) /*false*/) //disabling for dev
            {
                var version = boltUpdater.getUpdateVersion();
                Focus();
                var result = MessageBox.Show($"There is a newer Version of ReCT available!\nYour Version: {ReCT.info.Version}  New Version: {version}\n\nWould you like to update?", "ReCT Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                if (result == DialogResult.Yes)
                {
                    running = new Process();
                    running.StartInfo.FileName = "CMD.exe";
                    running.StartInfo.Arguments = "/K cd Bolt & update.cmd";

                    running.Start();

                    Environment.Exit(0);
                    return;
                }
            }

            Thread.Sleep(1500);

            InitializeComponent();
        }

        void SplashScreen()
        {
            Application.Run(new Startup());
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Activate();
            CenterToScreen();

            Menu.Renderer = new MenuRenderer();

            standardMsg += ReCT.info.Version;
            standardMsg += "\r\npackage sys;";

            errorBox = new Error();
            SetCodeBoxColors();
            fileChanged = false;
            //updateWindowTitle();

            icons[0] = Play.BackgroundImage;
            icons[1] = Stop.BackgroundImage;
            icons[2] = Build.BackgroundImage;

            icons[3] = Image.FromFile("res/playIconHL.png");
            icons[4] = Image.FromFile("res/literally_just_a_fukin_SquareIconHL.png");
            icons[5] = Image.FromFile("res/gearIconHL.png");

            icons[6] = Image.FromFile("res/playIconLoad.png");

            standardAC = ReCTAutoComplete.Items;

            TabPrefab = (Button)CtrlClone.ControlFactory.CloneCtrl(Tab);
            Tab.Dispose();
            Controls.Remove(Tab);

            stdBox = Cloner.DeepClone<FastColoredTextBox>(CodeBox);// (FastColoredTextBox)CtrlClone.ControlFactory.CloneCtrl(CodeBox);

            var tab = makeNewTab();
            tabs.Add(tab);

            settings = new Settings(this);
            settings.Hide();

            settings.autosave.SelectedIndex = Properties.Settings.Default.Autosave;
            settings.maximize.SelectedIndex = Properties.Settings.Default.Maximize ? 1 : 0;
            settings.maximizeRect.SelectedIndex = Properties.Settings.Default.MaximizeRect ? 1 : 0;         

            dc = new Discord();
            dc.Initialize();

            presence = new RichPresence()
            {
                Details = "Working on Untitled...",
                Timestamps = new Timestamps()
                {
                    Start = DateTime.UtcNow
                },
                Assets = new Assets()
                {
                    LargeImageKey = "rect",
                    LargeImageText = "ReCT IDE",
                }
            };

            presence.Details = "Working on " + tabs[currentTab].name + "...";
            dc.client.SetPresence(presence);

            OrderTabs();

            if (fileToOpen != "")
            {
                OpenFile(fileToOpen);
                fileToOpen = "";
            }
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
        Style DecimalStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 113, 120)), null, FontStyle.Regular);
        Style SystemFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(255, 131, 7)), null, FontStyle.Regular);
        Style UserFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(25, 189, 93)), null, FontStyle.Regular);
        Style VariableStyle = new TextStyle(new SolidBrush(Color.FromArgb(255, 212, 125)), null, FontStyle.Regular);
        Style TypeFunctionStyle = new TextStyle(new SolidBrush(Color.FromArgb(230, 115, 83)), null, FontStyle.Regular);
        Style CommentStyle = new TextStyle(new SolidBrush(Color.FromArgb(100, 100, 100)), null, FontStyle.Regular);
        Style WhiteStyle = new TextStyle(Brushes.White, null, FontStyle.Regular);
        Style SettingStyle = new TextStyle(new SolidBrush(Color.FromArgb(17, 191, 119)), null, FontStyle.Bold);
        Style PackageStyle = new TextStyle(new SolidBrush(Color.FromArgb(252, 186, 3)), null, FontStyle.Regular);
        Style ClassStyle = new TextStyle(new SolidBrush(Color.FromArgb(83, 230, 159)), null, FontStyle.Bold);


        Style DebugStyle = new TextStyle(new SolidBrush(Color.FromArgb(125, 125, 125)), null, FontStyle.Regular);

        public void ReloadHightlighting(TextChangedEventArgs e)
        {
            e.ChangedRange.ClearFoldingMarkers();

            //set folding markers [DarkMode]
            e.ChangedRange.SetFoldingMarkers("{", "}");

            //clear style of range [DarkMode]
            e.ChangedRange.ClearStyle(CommentStyle);
            //quotes
            e.ChangedRange.SetStyle(StringStyle, "\\\"(.*?)\\\"", RegexOptions.Singleline);

            //comment highlighting [DarkMode]
            e.ChangedRange.SetStyle(CommentStyle, @"//.*$", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(CommentStyle, @"/\*(.*?)\*/", RegexOptions.Singleline);

            e.ChangedRange.SetStyle(AttachStyle, @"(#attach\b|#copy\b|#copyFolder\b|#closeConsole\b)", RegexOptions.Singleline);

            //clear style of range [DarkMode]
            e.ChangedRange.ClearStyle(SystemFunctionStyle);

            //system function highlighting
            e.ChangedRange.SetStyle(SystemFunctionStyle, @"(\bListenOnTCPPort\b|\bConnectTCPClient\b|\bGetDirsInDirectory\b|\bGetFilesInDirectory\b|\bCreateDirectory\b|\bDeleteDirectory\b|\bDeleteFile\b|\bDirectoryExists\b|\bFileExists\b|\bWriteFile\b|\bReadFile\b|\bFloor\b|\bCeil\b|\bThread\b|\bRandom\b|\bVersion\b)");
            e.ChangedRange.SetStyle(SystemFunctionStyle, rectCompCheck.ImportedFunctions);

            //types
            e.ChangedRange.SetStyle(TypeStyle, @"(\b\?\b|\btcpsocketArr\b|\btcplistenerArr\b|\btcpclientArr\b|\btcpsocket\b|\btcplistener\b|\btcpclient\b|\bany\b|\bbool\b|\bint\b|\bstring\b|\bvoid\b|\bfloat\b|\bthread\b|\banyArr\b|\bboolArr\b|\bintArr\b|\bstringArr\b|\bfloatArr\b|\bthreadArr\b)");

            //statementHighlingting
            e.ChangedRange.SetStyle(VarStyle, @"(\bvar\b|\bset\b|\bif\b|\belse\b|\bfunction\b|\bclass\b|\btrue\b|\bfalse\b|\bmake\b|\barray\b|\bobject\b)", RegexOptions.Singleline);

            //settings
            e.ChangedRange.SetStyle(SettingStyle, @"(\bpackage\b|\bnamespace\b|\btype\b|\buse\b|\bdll\b)", RegexOptions.Singleline);

            //numbers
            e.ChangedRange.SetStyle(DecimalStyle, @"(?<=\.)\d+", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(NumberStyle, @"(\b\d+\b)", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(NumberStyle, @"(?<=\d)\.(?=\d)", RegexOptions.Multiline);

            //variables
            e.ChangedRange.SetStyle(VariableStyle, @"(\w+(?=\s+<-))");
            e.ChangedRange.SetStyle(VariableStyle, @"(\w+(?=\s+->))");
            e.ChangedRange.SetStyle(VariableStyle, rectCompCheck.Variables);

            //functions
            e.ChangedRange.SetStyle(UserFunctionStyle, @"(?<=\bfunction\s)(\w+)");
            e.ChangedRange.SetStyle(UserFunctionStyle, rectCompCheck.Functions);

            //classes
            e.ChangedRange.SetStyle(SettingStyle, rectCompCheck.Classes);

            //packages
            e.ChangedRange.SetStyle(PackageStyle, rectCompCheck.Namespaces);

            //package functions
            e.ChangedRange.SetStyle(SystemFunctionStyle, @"(\w*(?<=::)" + rectCompCheck.NamespaceFunctions + ")");

            //type functions
            e.ChangedRange.SetStyle(TypeFunctionStyle, @"(?<=\>>\s)(\w+)");

            //statements highlighting
            e.ChangedRange.SetStyle(StatementStyle, @"(\btry\b|\bcatch\b|\bbreak\b|\bcontinue\b|\bfor\b|\breturn\b|\bto\b|\bwhile\b|\bdo\b|\bdie\b|\bfrom\b)", RegexOptions.Singleline);

            //set standard text color
            e.ChangedRange.SetStyle(WhiteStyle, @".*", RegexOptions.Multiline);
        }

        Style ErrorMarker = new TextStyle(Brushes.White, Brushes.Red, FontStyle.Regular);
        void markError(TextChangedEventArgs e)
        {
            e.ChangedRange.SetStyle(ErrorMarker, ".*", RegexOptions.Multiline);
        }

        #endregion

        Tab makeNewTab()
        {
            var newTab = new Tab();
            newTab.button = (Button)CtrlClone.ControlFactory.CloneCtrl(TabPrefab);
            newTab.codebox = Cloner.DeepClone<FastColoredTextBox>(CodeBox);
            newTab.codebox.ClearUndo();
            newTab.codebox.Text = standardMsg;
            newTab.saved = true;
            newTab.button.Click += Tab_Click;
            newTab.button.FlatStyle = FlatStyle.Flat;
            newTab.button.FlatAppearance.BorderSize = 0;
            newTab.name = "Untitled";
            Controls.Add(newTab.button);
            return newTab;
        }

        void OrderTabs()
        {
            try
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    tabs[i].button.Location = new Point(5 + (100 * i), 35);
                    tabs[i].button.Text = tabs[i].name;

                    if (!tabs[i].saved)
                        tabs[i].button.Text += "*";

                    tabs[i].button.BackColor = Color.FromArgb(32, 32, 32);
                }
                tabs[currentTab].button.BackColor = Color.FromArgb(64, 41, 41);

                presence.Details = "Working on " + tabs[currentTab].name + "...";
                dc.client.SetPresence(presence);
            }
            catch { }
        }

        private void New_Click(object sender, EventArgs e)
        {
            tabs[currentTab].codebox = CodeBox;
            tabs.Add(makeNewTab());
            switchTab(tabs.Count - 1);
            tabs[currentTab].name = "Untitled";
            OrderTabs();

            //if (!fileChanged)
            //{
            //    CodeBox.Text = standardMsg;
            //    CodeBox.ClearUndo();
            //    openFile = "";
            //}
            //else
            //{
            //    var result = MessageBox.Show("You have unsaved changes!\nAre you sure you want to create a new File?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);https://pcpartpicker.com/user/RedCooooobe/saved/CdnKpg
            //    if (result == DialogResult.Yes)
            //    {
            //        CodeBox.Text = standardMsg;
            //        CodeBox.ClearUndo();
            //        fileChanged = false;
            //        openFile = "";
            //    }
            //}
            //updateWindowTitle();
        }

        private void Open_Click(object sender, EventArgs e)
        {
            //if(fileChanged)
            //{
            //    var result = MessageBox.Show("You have unsaved changes!\nAre you sure you want to open a File?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            //    if (result != DialogResult.Yes)
            //    {
            //        return;
            //    }
            //}

            openFileDialog1.Filter = "ReCT code files (*.rct)|*.rct|All files (*.*)|*.*";

            ((ToolStripMenuItem)sender).Owner.Hide();

            var res = openFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            OpenFile(openFileDialog1.FileName);
        }

        public void OpenFile(string path)
        {
            if (tabs.Count != 1 || tabs[0].name != "Untitled" || !tabs[0].saved)
            {
                tabs[currentTab].codebox = CodeBox;
                tabs.Add(makeNewTab());
                switchTab(tabs.Count - 1);
            }


            using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                CodeBox.Text = sr.ReadToEnd();
                CodeBox.ClearUndo();
                sr.Close();
            }

            tabs[currentTab].name = Path.GetFileName(path);
            tabs[currentTab].path = path;
            tabs[currentTab].saved = true;
            OrderTabs();

            Properties.Settings.Default.LastOpenFile = path;
            Properties.Settings.Default.Save();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if(tabs[currentTab].path == "" || tabs[currentTab].path == null)
            {
                SaveAs_Click(sender, e);
                return;
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(tabs[currentTab].path, FileMode.Create)))
            {
                sw.Write(CodeBox.Text);
                sw.Close();
            }

            tabs[currentTab].saved = true;

            OrderTabs();
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

            tabs[currentTab].path = saveFileDialog1.FileName;
            tabs[currentTab].name = Path.GetFileName(saveFileDialog1.FileName);
            tabs[currentTab].saved = true;
            OrderTabs();
        }


        void edited()
        {
            if (tabSwitch)
                return;

            try
            {
                if (tabs[currentTab].saved)
                {
                    tabs[currentTab].saved = false;
                    OrderTabs();
                }
            }
            catch{}
        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                rectCompCheck.Variables = "";
                if (CodeBox.Text != "")
                {
                    rectCompCheck.Check(CodeBox.Text, this, tabs[currentTab].path);
                    CodeBox.ClearStylesBuffer();
                    ReloadHightlighting(new TextChangedEventArgs(CodeBox.Range));

                    List<string> ACItems = new List<string>();

                    foreach (string s in standardAC)
                    {
                        ACItems.Add(s);
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in rectCompCheck.functions)
                    {
                        ACItems.Add(f.Name);
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.VariableSymbol v in rectCompCheck.variables)
                    {
                        ACItems.Add(v.Name);
                    }

                    foreach (ReCT.CodeAnalysis.Package.Package p in rectCompCheck.packages)
                    {
                        foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in p.scope.GetDeclaredFunctions())
                        {
                            ACItems.Add(p.name + "::" + f.Name);
                        }
                    }

                    ReCTAutoComplete.Items = ACItems.ToArray();
                }
            }
            catch(Exception ee)
            {
                ReCT_Compiler.inUse = false;
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
            saveFileDialog1.Filter = "Launcher (*.cmd)|*.cmd|Assembly (*.dll)|*.dll|All files (*.*)|*.*";
            var res = saveFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            if (fileChanged)
                Save_Click(this, new EventArgs());


            ReCT_Compiler.CompileRCTBC (saveFileDialog1.FileName, tabs[currentTab].path, errorBox);

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

            try
            {
                if(running != null)
                    KillProcessAndChildren(running.Id);
            }
            catch { }

            if (!tabs[currentTab].saved)
                Save_Click(this, new EventArgs());

            //clear Builder dir

            if (Directory.Exists("Builder"))
                ReCT_Compiler.ForceDeleteFilesAndFoldersRecursively("Builder");
            if (!Directory.Exists("Builder"))
                Directory.CreateDirectory("Builder");

            Console.WriteLine("----------------------------------------------------");

            if (!ReCT_Compiler.CompileRCTBC("Builder/" + Path.GetFileNameWithoutExtension(tabs[currentTab].path) + ".cmd", tabs[currentTab].path, errorBox)) return;

            string strCmdText = $"/K cd \"{Path.GetFullPath($"Builder")}\" & cls & \"{Path.GetFileNameWithoutExtension(tabs[currentTab].path)}.cmd\"";

            running = new Process();
            running.StartInfo.FileName = "CMD.exe";
            running.StartInfo.Arguments = strCmdText;

            if(Properties.Settings.Default.Maximize)
                running.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

            running.Start();
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

        private void Tab_Click(object sender, EventArgs e)
        {
            switchTab(findPressedTab(sender));
        }

        int findPressedTab(object button)
        {
            for(int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].button == (Button)button)
                    return i;
            }
            return 0;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (tabs.Count == 1)
                return;

            var cT = currentTab;

            if (cT == 0)
            {
                switchTab(1);
                currentTab = 0;
            }
            else switchTab(0);

            if (!tabs[cT].saved)
            {
                var result = MessageBox.Show("WAIT!\nYou have some unsaved changes!\nDo you want to save them?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning); https://pcpartpicker.com/user/RedCooooobe/saved/CdnKpg
                if (result == DialogResult.Yes)
                {
                    Save_Click(this, new EventArgs());
                }
                if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            Controls.Remove(tabs[cT].button);
            tabs.RemoveAt(cT);

            OrderTabs();
        }

        void switchTab(int tab)
        {
            try
            {
                presence.Details = "Working on " + tabs[currentTab].name + "...";
                dc.client.SetPresence(presence);
            }
            catch { }

            tabSwitch = true;

            if (tabs.Count == 1)
                return;

            if (tab != currentTab)
            {
                if (currentTab < tabs.Count)
                {
                    tabs[currentTab].codebox = CodeBox;
                    tabs[currentTab].button.BackColor = Color.FromArgb(32, 32, 32);
                }
                currentTab = tab;
                CodeBox.ClearUndo();
            }
            
            tabs[currentTab].button.BackColor = Color.FromArgb(64, 41, 41);
            CodeBox = tabs[currentTab].codebox;

            tabswitchTimer.Start();
        }

        private void tabswitchTimer_Tick(object sender, EventArgs e)
        {
            tabswitchTimer.Stop();
            tabSwitch = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(Tab t in tabs)
            {
                if (!t.saved)
                {
                    var result = MessageBox.Show($"WAIT!\nYou have some unsaved changes in '{t.name}'!\nDo you want to save them?", "Warning!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning); https://pcpartpicker.com/user/RedCooooobe/saved/CdnKpg
                    if (result == DialogResult.Yes)
                    {
                        switchTab(findPressedTab(t.button));
                        Save_Click(this, new EventArgs());
                    }
                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        public void updateFromSettings()
        {
            switch(Properties.Settings.Default.Autosave)
            {
                case 0:
                    Autosave.Stop();
                    break;
                case 1:
                    Autosave.Start();
                    Autosave.Interval = 60000;
                    break;
                case 2:
                    Autosave.Start();
                    Autosave.Interval = 60000 * 2;
                    break;
                case 3:
                    Autosave.Start();
                    Autosave.Interval = 60000 * 5;
                    break;
                case 4:
                    Autosave.Start();
                    Autosave.Interval = 60000 * 10;
                    break;
            }

            if (Properties.Settings.Default.MaximizeRect)
                this.WindowState = FormWindowState.Maximized;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Show();
        }

        private void Autosave_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("AutosaveTick!");
            if (tabs[currentTab].path != "" && tabs[currentTab].path != null)
                Save_Click(null, new EventArgs());
        }

        private void MaxTimer_Tick(object sender, EventArgs e)
        {
            ShowWindow(running.MainWindowHandle, 3);
            MaxTimer.Stop();
        }

        private void openLastFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.LastOpenFile != "")
            {
                if (tabs.Count != 1 || tabs[0].name != "Untitled" || !tabs[0].saved)
                {
                    tabs[currentTab].codebox = CodeBox;
                    tabs.Add(makeNewTab());
                    switchTab(tabs.Count - 1);
                }


                using (StreamReader sr = new StreamReader(new FileStream(Properties.Settings.Default.LastOpenFile, FileMode.Open)))
                {
                    CodeBox.Text = sr.ReadToEnd();
                    CodeBox.ClearUndo();
                    sr.Close();
                }

                tabs[currentTab].name = Path.GetFileName(Properties.Settings.Default.LastOpenFile);
                tabs[currentTab].path = Properties.Settings.Default.LastOpenFile;
                tabs[currentTab].saved = true;
                OrderTabs();
            }
        }

        private void reloadHighlightingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var code = CodeBox.Text;
            var pos = CodeBox.Selection;
            CodeBox.Text = "";
            reload(code, pos);
        }

        private async Task reload(string c, Range p)
        {
            await Task.Delay(10);
            CodeBox.Text = c;
            CodeBox.Selection = p;
            CodeBox.Focus();
        }

        private void forceRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            forceRun();
        }

        private async Task forceRun()
        {
            bool res;
            int counter = 0;
            errorBox.Hide();
            do
            {
                await Task.Delay(10);
                counter++;

                if (counter > 20)
                    break;


                try
                {
                    if (running != null)
                        KillProcessAndChildren(running.Id);
                }
                catch { }

                if (!tabs[currentTab].saved)
                    Save_Click(this, new EventArgs());

                //clear Builder dir

                if (Directory.Exists("Builder"))
                    ReCT_Compiler.ForceDeleteFilesAndFoldersRecursively("Builder");
                if (!Directory.Exists("Builder"))
                    Directory.CreateDirectory("Builder");

                res = ReCT_Compiler.CompileRCTBC("Builder/" + Path.GetFileNameWithoutExtension(tabs[currentTab].path) + ".cmd", tabs[currentTab].path, errorBox);
                if (!res) continue;

                errorBox.Hide();

                string strCmdText = $"/K cd \"{Path.GetFullPath($"Builder")}\" & cls & \"{Path.GetFileNameWithoutExtension(tabs[currentTab].path)}.cmd\"";

                running = new Process();
                running.StartInfo.FileName = "CMD.exe";
                running.StartInfo.Arguments = strCmdText;

                if (Properties.Settings.Default.Maximize)
                    running.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

                running.Start();

                return;
            } while (!res);
        }

        private void forceBuildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            forceBuild();
        }

        async Task forceBuild()
        {
            Typechecker.Enabled = false;
            errorBox.Hide();
            saveFileDialog1.Filter = "Launcher (*.cmd)|*.cmd|All files (*.*)|*.*";
            var res = saveFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            if (fileChanged)
                Save_Click(this, new EventArgs());

            bool succ;
            int counter = 0;
            do
            {
                counter++;
                succ = ReCT_Compiler.CompileRCTBC(saveFileDialog1.FileName, tabs[currentTab].path, errorBox);
                await Task.Delay(15);
            } while (!succ && counter < 20);

            if(succ)
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", saveFileDialog1.FileName));
            Typechecker.Enabled = true;
        }
    }

    class Tab
    {
        public Button button;
        public FastColoredTextBox codebox;
        public string name;
        public string path;
        public bool saved;
    }

    public static class Cloner
    {
        public static T DeepClone<T>(T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;

                return (T)formatter.Deserialize(stream);
            }
        }
    }
}

[System.Serializable]
public class fctb : FastColoredTextBox
{}
