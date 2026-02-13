using System;
using System.Windows.Forms;

namespace AutoTriage.Gui
{
    /// <summary>
    /// Application entry point for the AutoTriage.Gui project.
    /// 
    /// Responsibilities of this class:
    /// - Defines the single, unambiguous Main() method for the GUI application
    /// - Initializes WinForms application configuration
    /// - Launches the primary UI form (Form1)
    /// 
    /// Architectural note:
    /// This class is intentionally minimal. All UI logic lives in Form1,
    /// and all business / analysis logic lives in the AutoTriage.Core DLL.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Main entry point for the WinForms application.
        /// 
        /// [STAThread] is required for Windows Forms applications to ensure
        /// compatibility with COM components, Windows message handling,
        /// and UI threading requirements.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Configure High DPI mode for modern displays (Per-Monitor V2)
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                // Enable visual styles for modern Windows appearance
                Application.EnableVisualStyles();

                // Use compatible text rendering
                Application.SetCompatibleTextRenderingDefault(false);

                // Starts the application's message loop and displays Form1
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error during application startup:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Application Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
    }
}
