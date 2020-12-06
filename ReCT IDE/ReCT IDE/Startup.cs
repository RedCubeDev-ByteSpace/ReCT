using System;
using System.Threading;
using System.Windows.Forms;

namespace ReCT_IDE
{
    public partial class Startup : Form
    {
        public Startup()
        {
            InitializeComponent();
        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Close();
        }

        private void Startup_Load(object sender, EventArgs e)
        {
            var steps = 25;
            var stepsize = 100f / steps;

            for (float i = 0; i < steps; i ++)
            {
                Thread.Sleep(1);
                Opacity = stepsize * i / 100;
                Update();
            }
            Opacity = 1;
            Update();
        }
    }
}
