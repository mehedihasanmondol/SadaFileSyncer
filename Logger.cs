using System;
using System.IO;

namespace SadaFileSyncer
{
    public static class Logger
    {
        static readonly string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SadaFileSyncer", "logs");
        static readonly string logFile = Path.Combine(logDir, "activity.log");

        static Logger()
        {
            try { if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir); } catch { }
        }

        public static void Log(string msg)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {msg}";
                File.AppendAllText(logFile, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
