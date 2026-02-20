using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoTriage.Gui
{
    public sealed class Form1 : Form
    {
        public Form1()
        {
            Text = "AutoTriage - Main";
            Size = new Size(1200, 800);

            Controls.Add(new Label
            {
                Text = "Main UI restored. Next: re-add your full layout.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            });
        }
    }
}


