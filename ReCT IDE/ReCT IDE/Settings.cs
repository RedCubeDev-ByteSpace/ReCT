using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReCT_IDE
{
    public partial class Settings : Form
    {
        Form1 f1;
        public Settings(Form1 f)
        {
            f1 = f;
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {

        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            e.Cancel = true;
            this.Hide();
        }

        private void autosave_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Autosave = autosave.SelectedIndex;
            Properties.Settings.Default.Save();
            f1.updateFromSettings();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MaximizeRect = checkBox2.Checked;
            Properties.Settings.Default.Save();
            f1.updateFromSettings();
        }

        private void runProgramButton_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void rpButton_Click(object sender, EventArgs e)
        {
            KeysConverter kc = new KeysConverter();
            SettingsInfo.run = (Keys)kc.ConvertFromString(this.textBox1.Text);
        }
    }

    public static class SettingsInfo
    {
        public static System.Windows.Forms.Keys run = ((Keys)(System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R));
    }
}
