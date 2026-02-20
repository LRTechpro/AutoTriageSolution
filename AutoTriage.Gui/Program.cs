using System;
using System.Windows.Forms;

namespace AutoTriage.Gui
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new AutoTriage.Gui.Form1());
        }
    }
}
