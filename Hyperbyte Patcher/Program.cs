using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Hyperbyte_Patcher
{
    static class Program
    {

        private static bool enableNotice = true;
        private static string windowTitle = "Hyperbyte Patcher";
        private static string appExecutable = "";
        private static string appArguments = "";

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
            Application.SetCompatibleTextRenderingDefault(true);
            Application.Run(new FormPatcher(enableNotice, windowTitle, appExecutable, appArguments));
        }
    }
}
