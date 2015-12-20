using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Hyperbyte_Selfupdater
{
    static class Program
    {
        private static string patcherExecutable = "hyperbytepatcher.exe";
        private static string patcherArguments = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                MessageBox.Show("Already Running"); 
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormUpdater(patcherExecutable, patcherArguments));
        }
    }
}
