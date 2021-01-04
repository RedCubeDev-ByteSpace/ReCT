using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using Newtonsoft.Json;

namespace ReCT_IDE
{
    public partial class NewProject : Form
    {
        Form1 mainForm;

        public NewProject(Form1 f1)
        {
            mainForm = f1;
            InitializeComponent();
            var pos = this.PointToScreen(label1.Location);
            pos = pictureBox1.PointToClient(pos);
            label1.Parent = pictureBox1;
            label1.Location = pos;
            label1.BackColor = Color.Transparent;
        }

        private void NewProject_Load(object sender, EventArgs e)
        {
            location.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\ReCT Projects";
            pictureBox2.BackgroundImage = ResizeImage(pictureBox2.BackgroundImage, 64, 64);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                location.Text = folderBrowser.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFile.Filter = "PNG Images (*.png)|*.png|JPG Images (*.jpg)|*.jpg;*.jpeg";

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                var bmp = ResizeImage(Image.FromFile(openFile.FileName), 64, 64);
                pictureBox2.BackgroundImage = bmp;
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            location.BorderStyle = BorderStyle.None;
            projectName.BorderStyle = BorderStyle.None;
            mainName.BorderStyle = BorderStyle.None;
            Brush p = new SolidBrush(Color.FromArgb(25,25,25));
            Graphics g = e.Graphics;
            int variance = 3;
            int offset = 0;
            g.FillRectangle(p, new Rectangle(location.Location.X - variance, location.Location.Y - variance + offset, location.Width + variance * 2, location.Height + variance * 2));
            g.FillRectangle(p, new Rectangle(projectName.Location.X - variance, projectName.Location.Y - variance + offset, projectName.Width + variance * 2, projectName.Height + variance * 2));
            g.FillRectangle(p, new Rectangle(mainName.Location.X - variance, mainName.Location.Y - variance + offset, mainName.Width + variance * 2, mainName.Height + variance * 2));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //if (mainName.)

            //create project holder
            if (!Directory.Exists(location.Text))
                Directory.CreateDirectory(location.Text);

            var dirs = Directory.GetDirectories(location.Text);
            var dir = dirs.FirstOrDefault(x => x.Split('\\').Last() == projectName.Text);

            int index = 0;
            while (dir != null)
            {
                index++;
                dir = dirs.FirstOrDefault(x => x.Split('\\').Last() == projectName.Text + index);
            }

            //create project dir
            string projectDir = index == 0 ? location.Text + "\\" + projectName.Text : location.Text + "\\" + projectName.Text + index;

            Directory.CreateDirectory(projectDir);

            //create .rcp
            string base64Image;
            using (MemoryStream m = new MemoryStream())
            {
                var img = pictureBox2.BackgroundImage;
                img.Save(m, ImageFormat.Bmp);
                byte[] imageBytes = m.ToArray();
                base64Image = Convert.ToBase64String(imageBytes);
            }

            var proj = new Project(projectName.Text, base64Image, mainName.Text);
            string Json = JsonConvert.SerializeObject(proj);

            using (StreamWriter sw = new StreamWriter(new FileStream(projectDir + "\\" + projectName.Text + ".rcp", FileMode.OpenOrCreate)))
            {
                sw.Write(Json);
            }

            //create folders
            Directory.CreateDirectory(projectDir + "\\Classes");
            Directory.CreateDirectory(projectDir + "\\Resources");

            //create main class
            if (!mainName.Text.EndsWith(".rct"))
                mainName.Text += ".rct";

            using (StreamWriter sw = new StreamWriter(new FileStream(projectDir + "\\Classes\\" + mainName.Text, FileMode.OpenOrCreate)))
            {
                sw.WriteLine(mainForm.standardMsg.Split('\n')[0]);
                sw.WriteLine("// " + projectName.Text);
                sw.WriteLine("// Created on " + DateTime.Now.ToString() + "\n");
                sw.WriteLine(mainForm.standardMsg.Split('\n')[1]);
                sw.Write($"\n//Copying 'Resources' Folder...\n#copyFolder(\"{projectDir + "\\Resources"}\");");
            }

            mainForm.OpenFile(projectDir + "\\" + projectName.Text + ".rcp");
            Close();
        }
    }
}
