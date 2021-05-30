using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace ReCT_Installer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
             textBox1.Text = @"C:\Program Files\ReCT";
        }

        private void browse_Click(object sender, EventArgs e)
        {
            var res = folderBrowserDialog1.ShowDialog();
            if (res == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
                return;
            var strCmdText = "/C cd" + Directory.GetCurrentDirectory() + "Bolt && Bolt -i -p rect -d \"D:\\ReccInstaller\\\" && pause";
            Process.Start("CMD.exe ", strCmdText);

            richTextBox1.Text = "Installing";
            Process p = Process.Start(new ProcessStartInfo(Directory.GetCurrentDirectory() + "/Bolt/Bolt.exe")
            {
                Arguments = "-i -p rect -d " + textBox1.Text,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                
            });

            p.WaitForExit();
            richTextBox1.Text = "Done";

            MessageBox.Show("ReCT Installation Finished", "The ReCT Ide has been installed");
            
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
