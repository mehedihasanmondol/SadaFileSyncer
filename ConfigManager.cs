using System;
using System.IO;
using Newtonsoft.Json;

namespace SadaFileSyncer
{
    public static class ConfigManager
    {
        public static AppSettings Current = new AppSettings();
        static string path => AppSettings.SettingsPath();

        public static void Save()
        {
            try
            {
                var text = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(path, text);
            }
            catch { }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(path))
                {
                    var txt = File.ReadAllText(path);
                    Current = JsonConvert.DeserializeObject<AppSettings>(txt) ?? new AppSettings();
                }
            }
            catch { }
        }
    }
}
