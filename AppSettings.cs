using System;
using System.IO;
using System.Text.Json;

namespace SadaFileSyncer
{
    public class AppSettings
    {
        public string WatchFolder { get; set; } = string.Empty;
        public string DestFolder { get; set; } = string.Empty;
        public bool CopyMode { get; set; } = false;
        public bool OnlyNewFiles { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public bool SyncExistingFiles { get; set; } = false;

        public static string SettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SadaFileSyncer");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }
}
