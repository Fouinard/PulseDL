using PulseDL.src.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseDL.src.Managers
{
    internal class SettingsManager
    {
        private static string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PulseDL",
            "settings.json"
        );

        public static Settings Load()
        {
            if (!File.Exists(filePath))
            {
                return new Settings
                {
                    DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                    DefaultBrowser = "Sans navigateur",
                    AlwaysAskDlFolder = 0
                };
            }
            string json = File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<Settings>(json)!;
        }

        public static void Save(Settings settings)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, json);
        }
    }
}
