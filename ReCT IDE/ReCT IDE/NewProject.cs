using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReCT_IDE
{
    public partial class NewProject : Form
    {
        public NewProject()
        {
            InitializeComponent();
        }

        private void NewProject_Load(object sender, EventArgs e)
        {
            location.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\ReCT Projects";
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
            openFile.Filter = "PNG Images (*.png)|*.png|JPG Images (*.jpg)|*.jpg";

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                var bmp = ResizeImage(Image.FromFile(openFile.FileName), 64, 64);
                pictureBox2.Image = bmp;
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
            g.FillRectangle(p, new Rectangle(location.Location.X - variance, location.Location.Y - variance, location.Width + variance * 2, location.Height + variance * 2));
            g.FillRectangle(p, new Rectangle(projectName.Location.X - variance, projectName.Location.Y - variance, projectName.Width + variance * 2, projectName.Height + variance * 2));
            g.FillRectangle(p, new Rectangle(mainName.Location.X - variance, mainName.Location.Y - variance, mainName.Width + variance * 2, mainName.Height + variance * 2));
        }

        private void tbEnter(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void tbLeave(object sender, EventArgs e)
        {
            this.Refresh();
        }
    }
}
