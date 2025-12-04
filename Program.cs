using System;
using System.Windows.Forms;

namespace SadaFileSyncer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            ConfigManager.Load();
            Application.Run(new MainForm());
        }
    }
}
