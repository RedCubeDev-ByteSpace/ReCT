namespace ReCT_IDE
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.CodeBox = new FastColoredTextBoxNS.FastColoredTextBox();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.New = new System.Windows.Forms.ToolStripMenuItem();
            this.Open = new System.Windows.Forms.ToolStripMenuItem();
            this.openLastFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Save = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadHighlightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reCTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu = new System.Windows.Forms.MenuStrip();
            this.Build = new System.Windows.Forms.PictureBox();
            this.Stop = new System.Windows.Forms.PictureBox();
            this.Play = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.Typechecker = new System.Timers.Timer();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ReCTAutoComplete = new AutocompleteMenuNS.AutocompleteMenu();
            this.Tab = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabswitchTimer = new System.Windows.Forms.Timer(this.components);
            this.Autosave = new System.Windows.Forms.Timer(this.components);
            this.MaxTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.CodeBox)).BeginInit();
            this.Menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Build)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Stop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Play)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Typechecker)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // CodeBox
            // 
            this.CodeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CodeBox.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.ReCTAutoComplete.SetAutocompleteMenu(this.CodeBox, this.ReCTAutoComplete);
            this.CodeBox.AutoIndentCharsPatterns = "^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;=]+);\r\n^\\s*(case|default)\\s*[^:]*" +
    "(?<range>:)\\s*(?<range>[^;]+);";
            this.CodeBox.AutoScrollMinSize = new System.Drawing.Size(51, 14);
            this.CodeBox.BackBrush = null;
            this.CodeBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.CodeBox.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.CodeBox.CharHeight = 14;
            this.CodeBox.CharWidth = 8;
            this.CodeBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.CodeBox.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.CodeBox.Font = new System.Drawing.Font("Courier New", 9.75F);
            this.CodeBox.ForeColor = System.Drawing.Color.White;
            this.CodeBox.IsReplaceMode = false;
            this.CodeBox.Location = new System.Drawing.Point(0, 65);
            this.CodeBox.Name = "CodeBox";
            this.CodeBox.Paddings = new System.Windows.Forms.Padding(0);
            this.CodeBox.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.CodeBox.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("CodeBox.ServiceColors")));
            this.CodeBox.ShowFoldingLines = true;
            this.CodeBox.Size = new System.Drawing.Size(1088, 566);
            this.CodeBox.TabIndex = 1;
            this.CodeBox.Text = "kek";
            this.CodeBox.Zoom = 100;
            this.CodeBox.TextChanged += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.CodeBox_Chnaged);
            this.CodeBox.Load += new System.EventHandler(this.CodeBox_Load);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.BackColor = System.Drawing.Color.Transparent;
            this.fileToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.New,
            this.Open,
            this.Save,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(71, 29);
            this.fileToolStripMenuItem.Text = "File ▾";
            this.fileToolStripMenuItem.Click += new System.EventHandler(this.fileToolStripMenuItem_Click);
            // 
            // New
            // 
            this.New.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.New.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.New.ForeColor = System.Drawing.Color.White;
            this.New.Name = "New";
            this.New.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.New.Size = new System.Drawing.Size(252, 30);
            this.New.Text = "New";
            this.New.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.New.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.New.Click += new System.EventHandler(this.New_Click);
            // 
            // Open
            // 
            this.Open.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.Open.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Open.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openLastFileToolStripMenuItem});
            this.Open.ForeColor = System.Drawing.Color.White;
            this.Open.Name = "Open";
            this.Open.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.Open.Size = new System.Drawing.Size(252, 30);
            this.Open.Text = "Open";
            this.Open.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.Open.Click += new System.EventHandler(this.Open_Click);
            // 
            // openLastFileToolStripMenuItem
            // 
            this.openLastFileToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.openLastFileToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.openLastFileToolStripMenuItem.Name = "openLastFileToolStripMenuItem";
            this.openLastFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.openLastFileToolStripMenuItem.Size = new System.Drawing.Size(304, 30);
            this.openLastFileToolStripMenuItem.Text = "Open last File";
            this.openLastFileToolStripMenuItem.Click += new System.EventHandler(this.openLastFileToolStripMenuItem_Click);
            // 
            // Save
            // 
            this.Save.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.Save.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Save.ForeColor = System.Drawing.Color.White;
            this.Save.Name = "Save";
            this.Save.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.Save.Size = new System.Drawing.Size(252, 30);
            this.Save.Text = "Save";
            this.Save.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.Save.Click += new System.EventHandler(this.Save_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.saveAsToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(252, 30);
            this.saveAsToolStripMenuItem.Text = "Save as";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAs_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.BackColor = System.Drawing.Color.Transparent;
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoFormatToolStripMenuItem,
            this.reloadHighlightingToolStripMenuItem});
            this.editToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(75, 29);
            this.editToolStripMenuItem.Text = "Edit ▾";
            // 
            // autoFormatToolStripMenuItem
            // 
            this.autoFormatToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.autoFormatToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.autoFormatToolStripMenuItem.Name = "autoFormatToolStripMenuItem";
            this.autoFormatToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
            this.autoFormatToolStripMenuItem.Size = new System.Drawing.Size(303, 30);
            this.autoFormatToolStripMenuItem.Text = "Auto Format";
            this.autoFormatToolStripMenuItem.Click += new System.EventHandler(this.autoFormatToolStripMenuItem_Click);
            // 
            // reloadHighlightingToolStripMenuItem
            // 
            this.reloadHighlightingToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.reloadHighlightingToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.reloadHighlightingToolStripMenuItem.Name = "reloadHighlightingToolStripMenuItem";
            this.reloadHighlightingToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.reloadHighlightingToolStripMenuItem.Size = new System.Drawing.Size(303, 30);
            this.reloadHighlightingToolStripMenuItem.Text = "Reload Highlighting";
            this.reloadHighlightingToolStripMenuItem.Click += new System.EventHandler(this.reloadHighlightingToolStripMenuItem_Click);
            // 
            // reCTToolStripMenuItem
            // 
            this.reCTToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buildToolStripMenuItem,
            this.runToolStripMenuItem,
            this.settingsToolStripMenuItem});
            this.reCTToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.reCTToolStripMenuItem.Name = "reCTToolStripMenuItem";
            this.reCTToolStripMenuItem.Size = new System.Drawing.Size(84, 29);
            this.reCTToolStripMenuItem.Text = "ReCT ▾";
            // 
            // buildToolStripMenuItem
            // 
            this.buildToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.buildToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.buildToolStripMenuItem.Name = "buildToolStripMenuItem";
            this.buildToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.buildToolStripMenuItem.Size = new System.Drawing.Size(177, 30);
            this.buildToolStripMenuItem.Text = "Run";
            this.buildToolStripMenuItem.Click += new System.EventHandler(this.buildToolStripMenuItem_Click);
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.runToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(177, 30);
            this.runToolStripMenuItem.Text = "Build";
            this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.settingsToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(177, 30);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // Menu
            // 
            this.Menu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.Menu.Font = new System.Drawing.Font("Segoe UI", 13F);
            this.Menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.reCTToolStripMenuItem});
            this.Menu.Location = new System.Drawing.Point(0, 0);
            this.Menu.Name = "Menu";
            this.Menu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.Menu.Size = new System.Drawing.Size(1088, 33);
            this.Menu.TabIndex = 0;
            this.Menu.Text = "menuStrip1";
            this.Menu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.Menu_ItemClicked);
            // 
            // Build
            // 
            this.Build.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Build.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Build.BackgroundImage")));
            this.Build.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Build.Location = new System.Drawing.Point(1056, 4);
            this.Build.Name = "Build";
            this.Build.Size = new System.Drawing.Size(20, 26);
            this.Build.TabIndex = 2;
            this.Build.TabStop = false;
            this.ToolTip.SetToolTip(this.Build, "Build");
            this.Build.Click += new System.EventHandler(this.Build_Click);
            this.Build.MouseEnter += new System.EventHandler(this.Build_MouseEnter);
            this.Build.MouseLeave += new System.EventHandler(this.Build_MouseLeave);
            // 
            // Stop
            // 
            this.Stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Stop.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Stop.BackgroundImage")));
            this.Stop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Stop.Location = new System.Drawing.Point(1024, 4);
            this.Stop.Name = "Stop";
            this.Stop.Size = new System.Drawing.Size(15, 26);
            this.Stop.TabIndex = 3;
            this.Stop.TabStop = false;
            this.ToolTip.SetToolTip(this.Stop, "Stop running Program");
            this.Stop.Click += new System.EventHandler(this.Stop_Click);
            this.Stop.MouseEnter += new System.EventHandler(this.Stop_MouseEnter);
            this.Stop.MouseLeave += new System.EventHandler(this.Stop_MouseLeave);
            // 
            // Play
            // 
            this.Play.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Play.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Play.BackgroundImage")));
            this.Play.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Play.Location = new System.Drawing.Point(994, 4);
            this.Play.Name = "Play";
            this.Play.Size = new System.Drawing.Size(15, 26);
            this.Play.TabIndex = 4;
            this.Play.TabStop = false;
            this.ToolTip.SetToolTip(this.Play, "Run Program");
            this.Play.Click += new System.EventHandler(this.Play_Click);
            this.Play.MouseEnter += new System.EventHandler(this.Play_MouseEnter);
            this.Play.MouseLeave += new System.EventHandler(this.Play_MouseLeave);
            // 
            // Typechecker
            // 
            this.Typechecker.Enabled = true;
            this.Typechecker.Interval = 5000D;
            this.Typechecker.SynchronizingObject = this;
            this.Typechecker.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Elapsed);
            // 
            // ToolTip
            // 
            this.ToolTip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ToolTip.ForeColor = System.Drawing.SystemColors.HighlightText;
            // 
            // ReCTAutoComplete
            // 
            this.ReCTAutoComplete.AllowsTabKey = true;
            this.ReCTAutoComplete.AppearInterval = 100;
            this.ReCTAutoComplete.Colors = ((AutocompleteMenuNS.Colors)(resources.GetObject("ReCTAutoComplete.Colors")));
            this.ReCTAutoComplete.Font = new System.Drawing.Font("Liberation Mono", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReCTAutoComplete.ImageList = null;
            this.ReCTAutoComplete.Items = new string[] {
        "Print",
        "Input",
        "InputKey",
        "InputAction",
        "Random",
        "Version",
        "Clear",
        "SetCursor",
        "GetCursorX",
        "GetCursorY",
        "GetSizeX",
        "GetSizeY",
        "SetSize",
        "SetCursorVisible",
        "GetCursorVisible",
        "Thread",
        "SetConsoleBackground",
        "SetConsoleForeground",
        "Floor",
        "Ceil",
        "ReadFile",
        "WriteFile",
        "FileExists",
        "DirectoryExists",
        "DeleteFile",
        "DeleteDirectory",
        "CreateDirectory",
        "GetFilesInDirectory",
        "GetDirsInDirectory",
        "?",
        "any",
        "bool",
        "int",
        "string",
        "void",
        "float",
        "thread",
        "anyArr",
        "boolArr",
        "intArr",
        "stringArr",
        "floatArr",
        "threadArr",
        "var",
        "set",
        "if",
        "else",
        "function",
        "true",
        "false",
        "set",
        "break",
        "continue",
        "for",
        "return",
        "to",
        "while",
        "do",
        "end",
        "from",
        "Write",
        "Sleep",
        "GetLength",
        "Substring",
        "StartThread",
        "KillThread",
        "GetArrayLength",
        "#attach"};
            this.ReCTAutoComplete.LeftPadding = 0;
            this.ReCTAutoComplete.MaximumSize = new System.Drawing.Size(200, 200);
            this.ReCTAutoComplete.TargetControlWrapper = null;
            // 
            // Tab
            // 
            this.Tab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.Tab.FlatAppearance.BorderSize = 0;
            this.Tab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Tab.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Tab.ForeColor = System.Drawing.Color.White;
            this.Tab.Location = new System.Drawing.Point(5, 35);
            this.Tab.Name = "Tab";
            this.Tab.Size = new System.Drawing.Size(95, 34);
            this.Tab.TabIndex = 5;
            this.Tab.Text = "Untitled*";
            this.Tab.UseVisualStyleBackColor = false;
            this.Tab.Click += new System.EventHandler(this.Tab_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(1061, 44);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(15, 15);
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // tabswitchTimer
            // 
            this.tabswitchTimer.Interval = 500;
            this.tabswitchTimer.Tick += new System.EventHandler(this.tabswitchTimer_Tick);
            // 
            // Autosave
            // 
            this.Autosave.Tick += new System.EventHandler(this.Autosave_Tick);
            // 
            // MaxTimer
            // 
            this.MaxTimer.Tick += new System.EventHandler(this.MaxTimer_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.ClientSize = new System.Drawing.Size(1088, 631);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.CodeBox);
            this.Controls.Add(this.Tab);
            this.Controls.Add(this.Play);
            this.Controls.Add(this.Stop);
            this.Controls.Add(this.Build);
            this.Controls.Add(this.Menu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.Menu;
            this.Name = "Form1";
            this.Text = "ReCT IDE";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.CodeBox)).EndInit();
            this.Menu.ResumeLayout(false);
            this.Menu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Build)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Stop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Play)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Typechecker)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private FastColoredTextBoxNS.FastColoredTextBox CodeBox;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem New;
        private System.Windows.Forms.ToolStripMenuItem Open;
        private System.Windows.Forms.ToolStripMenuItem Save;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reCTToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem buildToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.MenuStrip Menu;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.PictureBox Build;
        private System.Windows.Forms.PictureBox Stop;
        private System.Windows.Forms.PictureBox Play;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Timers.Timer Typechecker;
        private System.Windows.Forms.ToolStripMenuItem autoFormatToolStripMenuItem;
        private System.Windows.Forms.ToolTip ToolTip;
        private AutocompleteMenuNS.AutocompleteMenu ReCTAutoComplete;
        private System.Windows.Forms.Button Tab;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer tabswitchTimer;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.Timer Autosave;
        private System.Windows.Forms.Timer MaxTimer;
        private System.Windows.Forms.ToolStripMenuItem openLastFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadHighlightingToolStripMenuItem;
    }
}

