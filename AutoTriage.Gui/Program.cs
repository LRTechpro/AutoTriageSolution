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
            // Initializes application-wide configuration such as:
            // - Default fonts
            // - High DPI settings
            // - Visual styles
            // This call is required for modern WinForms behavior.
            ApplicationConfiguration.Initialize();

            // Starts the application's message loop and displays Form1
            // as the main user interface window.
            Application.Run(new Form1());
        }
    }
}
