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
using Newtonsoft.Json;
using System.Drawing.Text;

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
        List<AutocompleteMenuNS.AutocompleteItem> standardAC = new List<AutocompleteMenuNS.AutocompleteItem>();
        PrivateFontCollection collection = new PrivateFontCollection();
        BoltUpdater boltUpdater;

        public Project openProject;
        public string projectPath;
        public ToolStripDropDownItem menuItem;

        public static string fileToOpen = "";

        public string head = "";
        public static string[][] terms;
        public string types = "";

        public static bool closing = false;

        Discord dc;
        RichPresence presence;

        Settings settings;

        bool tabSwitch = false;

        public System.Windows.Forms.Button TabPrefab;

        public Image[] icons = new Image[7];

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int cmdShow);


        public string standardMsg = "//ReCT Compiler and IDE ";

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
            Thread updateKeys = new Thread(new ThreadStart(UpdateKeys));
            updateKeys.Start();
        }

        void UpdateKeys()
        {
            while (!closing)
            {
                this.buildToolStripMenuItem.ShortcutKeys = SettingsInfo.run;
                Thread.Sleep(1000);
            }
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
            try
            {
                Activate();
                CenterToScreen();

                if (Properties.Settings.Default.LastOpenFiles == null) Properties.Settings.Default.LastOpenFiles = new System.Collections.Specialized.StringCollection();
                if (Properties.Settings.Default.LastOpenProjects == null) Properties.Settings.Default.LastOpenProjects = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Save();

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

                autocompleteImageList.Images.Add(Image.FromFile("res/typeIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/statementIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/functionIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/typefunctionIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/flagIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/variableIcon.png"));
                autocompleteImageList.Images.Add(Image.FromFile("res/classIcon.png"));

                setAC();

                TabPrefab = (System.Windows.Forms.Button)CtrlClone.ControlFactory.CloneCtrl(Tab);
                Tab.Dispose();
                Controls.Remove(Tab);

                var tab = makeNewTab();
                tabs.Add(tab);

                settings = new Settings(this);
                settings.Hide();

                settings.autosave.SelectedIndex = Properties.Settings.Default.Autosave;
                settings.checkBox2.Checked = Properties.Settings.Default.MaximizeRect;

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
            catch(Exception ex)
            {
                File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/logs/crash.log", ex.Message);
                new Crash().Show();
            }
        }

        void setAC()
        {
            string[][] acs =
            {
                new[]{ "any", "bool", "int", "byte", "string", "void", "float", "thread", "anyArr", "boolArr", "intArr", "byteArr", "stringArr", "floatArr", "threadArr", "action" },
                new[]{ "var", "set", "inc", "if", "else", "function", "class", "true", "false", "break", "continue", "for", "from", "to", "return", "while", "die", "ser", "abs", "is", "virt", "ovr", "alias" },
                new[]{ "Thread", "Constructor", "Action"},
                new[]{ "->GetLength", "->Substring", "->StartThread", "->KillThread", "->Open", "->Write", "->WriteLine", "->Read", "->ReadLine", "->IsConnected", "->Close", "->Push", "->GetBit", "->SetBit", "-Pop", "->At" },
                new[]{ "#attach", "#copy", "#copyFolder", "#closeConsole", "#noConsole" }
            };

            terms = acs;
            types = "(";
            for(int i = 0; i < acs.GetLength(0); i++)
            {
                types += acs[0][i] + "|";
            }
            types = types.Substring(0, types.Length - 1);
            types += ")";

            for (int type = 0; type < acs.Length; type++)
            {
                for (int i = 0; i < acs[type].Length; i++)
                {
                    standardAC.Add(new AutocompleteMenuNS.AutocompleteItem() { ImageIndex = type, Text = acs[type][i] });
                }
            }

            ReCTAutoComplete.SetAutocompleteItems(standardAC);
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

            collection.AddFontFile(Path.GetFullPath(@"res\libmono.ttf"));
            
            FontFamily fontFamily = new FontFamily("Liberation Mono", collection);

            CodeBox.Font = new Font(fontFamily, 20);
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
        Style ObjectLiteral = new TextStyle(new SolidBrush(Color.FromArgb(63, 78, 84)), null, FontStyle.Regular);


        Style DebugStyle = new TextStyle(new SolidBrush(Color.FromArgb(125, 125, 125)), null, FontStyle.Regular);

        public void ReloadHightlighting(TextChangedEventArgs e)
        {
            e.ChangedRange.ClearFoldingMarkers();
            e.ChangedRange.ClearStyle(UserFunctionStyle);
            e.ChangedRange.ClearStyle(CommentStyle);
            e.ChangedRange.ClearStyle(PackageStyle);
            e.ChangedRange.ClearStyle(ClassStyle);
            e.ChangedRange.ClearStyle(StringStyle);

            //set folding markers [DarkMode]
            e.ChangedRange.SetFoldingMarkers("{", "}");

            //quotes
            e.ChangedRange.SetStyle(StringStyle, "\\\"(.*?)\\\"", RegexOptions.Singleline);

            //comment highlighting [DarkMode]
            e.ChangedRange.SetStyle(CommentStyle, @"//.*$", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(CommentStyle, @"/\*(.*?)\*/", RegexOptions.Singleline);

            e.ChangedRange.SetStyle(AttachStyle, @"(#attach\b|#copy\b|#copyFolder\b|#closeConsole\b|#noConsole\b)", RegexOptions.Singleline);

            //system function highlighting
            e.ChangedRange.SetStyle(SystemFunctionStyle, @"(\b(Version|Thread|Action)\b)");
            e.ChangedRange.SetStyle(SystemFunctionStyle, rectCompCheck.ImportedFunctions);

            //types
            e.ChangedRange.SetStyle(TypeStyle, @"(\b\?\b|\bany\b|\bbool\b|\bint\b|\bbyte\b|\bstring\b|\bvoid\b|\bfloat\b|\bthread\b|\banyArr\b|\bboolArr\b|\bintArr\b|\bbyteArr\b|\bstringArr\b|\bfloatArr\b|\bthreadArr\b|\baction\b)");

            //statementHighlingting
            e.ChangedRange.SetStyle(VarStyle, @"(\bvar\b|\bset\b|\binc\b|\bif\b|\belse\b|\bfunction\b|\bclass\b|\btrue\b|\bfalse\b|\bmake\b|\barray\b|\bobject\b|\babs\b|\bvirt\b|\bovr\b)", RegexOptions.Singleline);

            //settings
            e.ChangedRange.SetStyle(SettingStyle, @"(\bpackage\b|\bnamespace\b|\btype\b|\buse\b|\bdll\b|\balias\b)", RegexOptions.Singleline);

            //numbers
            e.ChangedRange.SetStyle(DecimalStyle, @"(?<=\.)\d+", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(NumberStyle, @"(\b\d+\b)", RegexOptions.Singleline);
            e.ChangedRange.SetStyle(NumberStyle, @"(?<=\d)\.(?=\d)", RegexOptions.Singleline);

            //null / nil
            e.ChangedRange.SetStyle(ObjectLiteral, @"(\b(null|nil)\b)", RegexOptions.Singleline);

            //variables
            e.ChangedRange.ClearStyle(VariableStyle);

            e.ChangedRange.SetStyle(VariableStyle, @"(\w+(?=\s+<-))");
            e.ChangedRange.SetStyle(VariableStyle, @$"((?<=\()\w+(?=\s+{types})|(?<=,)(\s|)\w+(?=\s+{types}))");
            e.ChangedRange.SetStyle(VariableStyle, rectCompCheck.Variables);

            //functions
            e.ChangedRange.SetStyle(UserFunctionStyle, @"(?<=\bfunction\s)(\w+)");
            e.ChangedRange.SetStyle(UserFunctionStyle, rectCompCheck.Functions);

            //classes
            e.ChangedRange.SetStyle(SettingStyle, rectCompCheck.Classes);
            e.ChangedRange.SetStyle(SettingStyle, @"(\bMain\b)");

            //packages
            e.ChangedRange.SetStyle(PackageStyle, rectCompCheck.Namespaces);

            //package functions
            e.ChangedRange.SetStyle(SystemFunctionStyle, @"(\w*(?<=::)" + rectCompCheck.NamespaceFunctions + ")");

            //type functions
            e.ChangedRange.SetStyle(TypeFunctionStyle, @"(?<=\->(\s|))(\w+)");

            //statements highlighting
            e.ChangedRange.SetStyle(StatementStyle, @"(\b(try|catch|break|continue|for|return|to|while|do|die|from)\b)", RegexOptions.Singleline);

            //set standard text color
            e.ChangedRange.SetStyle(WhiteStyle, @".*", RegexOptions.Singleline);
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
            newTab.button = (System.Windows.Forms.Button)CtrlClone.ControlFactory.CloneCtrl(TabPrefab);
            newTab.code = standardMsg;
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

                    tabs[i].button.BackColor = Color.FromArgb(32, 32, 32);

                    if (!tabs[i].saved)
                    {
                        tabs[i].button.Text += "*";
                        tabs[i].button.BackColor = Color.FromArgb(45, 59, 64);
                    }

                    if (tabs[i].path == head)
                        tabs[i].button.BackColor = Color.FromArgb(45, 64, 49);
                }
                tabs[currentTab].button.BackColor = Color.FromArgb(64, 41, 41);

                presence.Details = "Working on " + tabs[currentTab].name + "...";
                dc.client.SetPresence(presence);
            }
            catch { }
        }

        private void New_Click(object sender, EventArgs e)
        {
            tabs[currentTab].code = CodeBox.Text;
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

            openFileDialog1.Filter = "ReCT files (*.rct / *.rcp)|*.rct;*.rcp|All files (*.*)|*.*";

            ((ToolStripMenuItem)sender).Owner.Hide();

            var res = openFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            OpenFile(openFileDialog1.FileName);
        }

        public void OpenFile(string path)
        {
            if (path.EndsWith(".rcp"))
            {
                OpenProject(path);
                return;
            }

            if (tabs.Count != 1 || tabs[0].name != "Untitled" || !tabs[0].saved)
            {
                tabs[currentTab].code = CodeBox.Text;
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

            if (openProject == null)
            {

                if (Properties.Settings.Default.LastOpenFiles.Contains(path)) Properties.Settings.Default.LastOpenFiles.Remove(path);
                Properties.Settings.Default.LastOpenFiles.Insert(0, path);

                if (Properties.Settings.Default.LastOpenFiles.Count > 5)
                    for (int i = 5; i < Properties.Settings.Default.LastOpenFiles.Count; i++)
                        Properties.Settings.Default.LastOpenFiles.RemoveAt(i);

                Properties.Settings.Default.Save();
            }
        }

        public void OpenProject(string path)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (!tabs[i].saved)
                {
                    switchTab(i);
                    Save_Click(null, null);
                }
                Controls.Remove(tabs[i].button);
            }

            tabs.Clear();
            var emptyTab = makeNewTab();
            emptyTab.name = "Untitled";
            tabs.Add(emptyTab);
            currentTab = 0;
            OrderTabs();

            var projJson = "";

            using (StreamReader sr = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                projJson = sr.ReadToEnd();
            }

            var project = JsonConvert.DeserializeObject<Project>(projJson);
            var projectPath = path.Replace("\\" + Path.GetFileName(path), "");
            this.projectPath = projectPath;
            this.openProject = project;

            this.project.Visible = true;
            this.project.Text = project.Name;
            this.project.Image = Image.FromStream(new MemoryStream(Convert.FromBase64String(project.Icon)));

            OpenFile(projectPath + "\\Classes\\" + project.MainClass);
            head = projectPath + "\\Classes\\" + project.MainClass;

            menuItem = MenuItem;

            RefreshClassMenu();
            OrderTabs();

            if (Properties.Settings.Default.LastOpenProjects.Contains(path)) Properties.Settings.Default.LastOpenProjects.Remove(path);
            Properties.Settings.Default.LastOpenProjects.Insert(0, path);

            if (Properties.Settings.Default.LastOpenProjects.Count > 5)
                for (int i = 5; i < Properties.Settings.Default.LastOpenProjects.Count; i++)
                    Properties.Settings.Default.LastOpenProjects.RemoveAt(i);

            Properties.Settings.Default.Save();
        }

        void RefreshClassMenu()
        {
            this.project.DropDownItems.Clear();

            var classes = Directory.GetFiles(projectPath + "\\Classes");
            var image = Image.FromFile("res/rct.ico");
            foreach (string s in classes)
            {
                var item = new ToolStripMenuItem();
                item.Text = Path.GetFileName(s);
                item.Name = Path.GetFileName(s);
                item.Click += ProjectItem_Click;
                item.ForeColor = menuItem.ForeColor;
                item.BackColor = menuItem.BackColor;
                item.Image = image;
                item.ImageAlign = ContentAlignment.BottomRight;
                item.Margin = new Padding(0,0,0,0);
                this.project.DropDownItems.Add(item);
            }

            var addClass = new ToolStripMenuItem();
            addClass.Text = "Add File";
            addClass.Name = "Add File";
            addClass.Click += AddClass_Click;
            addClass.ForeColor = menuItem.ForeColor;
            addClass.BackColor = menuItem.BackColor;
            addClass.Margin = new Padding(0,0,0,0);
            this.project.DropDownItems.Add(addClass);
        }

        private void AddClass_Click(object sender, EventArgs e)
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Add File to Project", "New File", "newFile.rct");
            if (name == "") return;

            if (!name.EndsWith(".rct")) name += ".rct";
            using (StreamWriter sw = new StreamWriter(new FileStream(projectPath + "\\Classes\\" + name, FileMode.OpenOrCreate)))
            {
                sw.WriteLine("// " + name.Replace(".rct", "") + " in " + openProject.Name);
            }

            OpenFile(projectPath + "\\Classes\\" + name);
            RefreshClassMenu();
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

            if (openProject == null)
            {
                if (Properties.Settings.Default.LastOpenFiles.Contains(tabs[currentTab].path)) Properties.Settings.Default.LastOpenFiles.Remove(tabs[currentTab].path);
                Properties.Settings.Default.LastOpenFiles.Insert(0, tabs[currentTab].path);

                if (Properties.Settings.Default.LastOpenFiles.Count > 5)
                    for (int i = 5; i < Properties.Settings.Default.LastOpenFiles.Count; i++)
                        Properties.Settings.Default.LastOpenFiles.RemoveAt(i);

                Properties.Settings.Default.Save();
            }

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

            if (openProject == null)
            {
                if (Properties.Settings.Default.LastOpenFiles.Contains(tabs[currentTab].path)) Properties.Settings.Default.LastOpenFiles.Remove(tabs[currentTab].path);
                Properties.Settings.Default.LastOpenFiles.Insert(0, tabs[currentTab].path);

                if (Properties.Settings.Default.LastOpenFiles.Count > 5)
                    for (int i = 5; i < Properties.Settings.Default.LastOpenFiles.Count; i++)
                        Properties.Settings.Default.LastOpenFiles.RemoveAt(i);

                Properties.Settings.Default.Save();
            }
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
                    var code = CodeBox.Text;

                    if (head != "" && tabs[currentTab].path != head)
                    {
                        var mainTab = tabs.FirstOrDefault(x => x.path == head);

                        if (mainTab != null) code = mainTab.code;
                        else
                            using (StreamReader sr = new StreamReader(new FileStream(head, FileMode.Open)))
                                code = sr.ReadToEnd();
                    }

                    rectCompCheck.Check(code, this, head == "" ? tabs[currentTab].path : head);
                    reloadHighlightingToolStripMenuItem_Click(null, null);

                    List<AutocompleteMenuNS.AutocompleteItem> ACItems = new List<AutocompleteMenuNS.AutocompleteItem>(standardAC);

                    foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in rectCompCheck.functions)
                    {
                        ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(f.Name, 2));
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.VariableSymbol v in rectCompCheck.variables)
                    {
                        ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(v.Name, 5));
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.ClassSymbol c in rectCompCheck.classes)
                    {
                        ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(c.Name, 6));
                    }

                    foreach (ReCT.CodeAnalysis.Package.Package p in rectCompCheck.packages)
                    {
                        foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in p.scope.GetDeclaredFunctions())
                        {
                            ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(p.name + "::" + f.Name, 2));
                        }
                        foreach (ReCT.CodeAnalysis.Symbols.ClassSymbol c in p.scope.GetDeclaredClasses())
                        {
                            ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(p.name + "::" + c.Name, 6));
                        }
                    }

                    foreach (ReCT.CodeAnalysis.Symbols.FunctionSymbol f in rectCompCheck.importFunctions)
                    {
                        ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(f.Name, 2));
                    }
                    foreach (ReCT.CodeAnalysis.Symbols.ClassSymbol c in rectCompCheck.importClasses)
                    {
                        ACItems.Add(new AutocompleteMenuNS.AutocompleteItem(c.Name, 6));
                    }

                    ReCTAutoComplete.SetAutocompleteItems(ACItems);
                    //ReloadHightlighting(new TextChangedEventArgs(CodeBox.VisibleRange));
                    //CodeBox.Update();
                    //reload();
                }
            }
            catch(Exception ee)
            {
                ReCT_Compiler.inUse = false;
                //Console.WriteLine("ded");
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
            saveFileDialog1.Filter = "Executable (*.exe)|*.exe|Assembly (*.dll)|*.dll|All files (*.*)|*.*";
            var res = saveFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            if (fileChanged)
                Save_Click(this, new EventArgs());


            ReCT_Compiler.CompileRCTBC (saveFileDialog1.FileName, head == "" ? tabs[currentTab].path : head, errorBox);

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

            var file = head == "" ? tabs[currentTab].path : head;
            var outfile = openProject == null ? Path.GetFileNameWithoutExtension(tabs[currentTab].path) : openProject.Name;

            if (!ReCT_Compiler.CompileRCTBC("Builder\\" + outfile + ".exe", file, errorBox)) return;

            //string strCmdText = $"/K cd \"{Path.GetFullPath($"Builder")}\" & cls & \"{Path.GetFileNameWithoutExtension(tabs[currentTab].path)}.exe\"";

            running = new Process();
            running.StartInfo.FileName = Path.GetFullPath("Builder/" + outfile + ".exe");

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
                if (tabs[i].button == (System.Windows.Forms.Button)button)
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

            tabs[currentTab].code = CodeBox.Text;

            if (tab != currentTab)
            {
                if (currentTab < tabs.Count)
                {
                    tabs[currentTab].button.BackColor = Color.FromArgb(32, 32, 32);
                }
                currentTab = tab;
                CodeBox.ClearUndo();
            }
            
            tabs[currentTab].button.BackColor = Color.FromArgb(64, 41, 41);
            CodeBox.Text = tabs[currentTab].code;

            tabswitchTimer.Start();
            OrderTabs();
        }

        private void tabswitchTimer_Tick(object sender, EventArgs e)
        {
            tabswitchTimer.Stop();
            tabSwitch = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;

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

        private void reloadHighlightingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadHightlighting(new TextChangedEventArgs(CodeBox.Range));
            CodeBox.Refresh();
            CodeBox.Update();
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

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newProject = new NewProject(this);
            newProject.Show();
        }

        private void setAsHeadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            head = tabs[currentTab].path;
        }

        private void ProjectItem_Click(object sender, EventArgs e)
        {
            var item = (ToolStripDropDownItem)sender;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].path == projectPath + "\\Classes\\" + item.Text)
                {
                    switchTab(i);
                    return;
                }
            }

            OpenFile(projectPath + "\\Classes\\" + item.Text);
        }

        private void histroryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var history = new History(this);
            history.Show();
        }

        private void duplicateLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var text = CodeBox.GetLine(CodeBox.Selection.ToLine);

            var index = 0;
            for (int i = 0; i <= text.ToLine; i++)
                index += CodeBox.GetLineLength(i);

            //Console.WriteLine(CodeBox.get);
            CodeBox.Selection.Start = new Place(CodeBox.GetLineLength(text.ToLine), text.ToLine);
            CodeBox.Selection.End = new Place(CodeBox.GetLineLength(text.ToLine), text.ToLine);
            CodeBox.InsertText("\n" + CodeBox.GetLineText(text.ToLine));
        }

        private void removeLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<int> line = new List<int>();
            line.Add(CodeBox.GetLine(CodeBox.Selection.ToLine).ToLine);
            var ln = CodeBox.GetLine(CodeBox.Selection.ToLine - 1);

            CodeBox.RemoveLines(line);
            CodeBox.Selection.Start = new Place(CodeBox.GetLineLength(ln.ToLine), ln.ToLine);
            CodeBox.Selection.End = new Place(CodeBox.GetLineLength(ln.ToLine), ln.ToLine);
        }

        private void ReCTAutoComplete_Selected(object sender, AutocompleteMenuNS.SelectedEventArgs e)
        {

        }

        private void reCTToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }

    class Tab
    {
        public System.Windows.Forms.Button button;
        public string code;
        public string name;
        public string path;
        public bool saved;
    }

    class EllipseStyle : Style
    {
        public override void Draw(Graphics gr, Point position, Range range)
        {
            Size size = GetSizeOfRange(range);
            Rectangle rect = new Rectangle(new Point(position.X, position.Y - size.Height), new Size(size.Width, 2));
            gr.FillRectangle(new SolidBrush(Color.Red), rect);
        }
    }
}
