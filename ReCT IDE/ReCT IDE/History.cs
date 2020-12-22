using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Linq;

namespace ReCT_IDE
{
    public partial class History : Form
    {
        Form1 Main;
        public History(Form1 main)
        {
            Main = main;
            InitializeComponent();
            var pos = this.PointToScreen(label2.Location);
            pos = pictureBox1.PointToClient(pos);
            label2.Parent = pictureBox1;
            label2.Location = pos;
            label2.BackColor = Color.Transparent;
        }

        private void History_Load(object sender, EventArgs e)
        {
            var lastFiles = Properties.Settings.Default.LastOpenFiles;
            var lastProjects = Properties.Settings.Default.LastOpenProjects;

            FileView.Items.Clear();
            for (int i = 0; i < lastFiles.Count; i++)
            {
                var item = new ListViewItem();
                item.Text = Path.GetFileName(lastFiles[i]);
                item.ImageIndex = 0;
                item.Font = new Font(item.Font.FontFamily, 12, FontStyle.Regular);
                item.ForeColor = Color.White;
                item.BackColor = Color.FromArgb(26,26,26);
                FileView.Items.Add(item);
            }

            ProjectView.Items.Clear();
            for (int i = 0; i < lastProjects.Count; i++)
            {
                using (StreamReader sr = new StreamReader(new FileStream(lastProjects[i], FileMode.Open)))
                {
                    var projJson = sr.ReadToEnd();
                    var project = JsonConvert.DeserializeObject<Project>(projJson);
                    imageList1.Images.Add(Image.FromStream(new MemoryStream(Convert.FromBase64String(project.Icon))));
                }

                var item = new ListViewItem();
                item.Text = Path.GetFileName(lastProjects[i]);
                item.ImageIndex = i + 1;
                item.Font = new Font(item.Font.FontFamily, 12, FontStyle.Regular);
                item.ForeColor = Color.White;
                item.BackColor = Color.FromArgb(26, 26, 26);
                ProjectView.Items.Add(item);
            }
        }

        private void OpenRecentFile(object sender, EventArgs e)
        {
            var lastFiles = Properties.Settings.Default.LastOpenFiles;
            
            foreach (string s in lastFiles)
            {
                if (Path.GetFileName(s) == (FileView.SelectedItems[0]).Text)
                {
                    Main.OpenFile(s);
                    Close();
                    return;
                }
            }
        }

        private void OpenRecentProject(object sender, EventArgs e)
        {
            var lastProjects = Properties.Settings.Default.LastOpenProjects;

            foreach (string s in lastProjects)
            {
                if (Path.GetFileName(s) == (ProjectView.SelectedItems[0]).Text)
                {
                    Main.OpenFile(s);
                    Close();
                    return;
                }
            }
        }

        private void FileView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
