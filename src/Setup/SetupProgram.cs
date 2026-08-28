using System;
using System.IO;
using System.Windows;

namespace ScrollIt.Setup
{
    public static class SetupProgram
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application app = new Application();

            bool isUninstall = false;

            // Check executable name (e.g. if copied as Uninstall.exe)
            string currentExeName = Path.GetFileName(AppDomain.CurrentDomain.FriendlyName ?? "").ToLowerInvariant();
            if (currentExeName.Contains("uninstall") || currentExeName.Contains("desinstall"))
            {
                isUninstall = true;
            }

            // Check command line arguments
            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (arg.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-u", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("/u", StringComparison.OrdinalIgnoreCase))
                    {
                        isUninstall = true;
                        break;
                    }
                }
            }

            if (isUninstall)
            {
                UninstallWindow window = new UninstallWindow();
                app.Run(window);
            }
            else
            {
                SetupWindow window = new SetupWindow();
                app.Run(window);
            }
        }
    }
}
