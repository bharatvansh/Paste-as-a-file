using System;
using System.IO;
using System.Text.Json;

namespace PasteIt.Core
{
    public sealed class SettingsManager
    {
        private static readonly string DataDirectory =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY"))
                ? Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY")!
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasteIt");

        private static readonly string SettingsFilePath =
            Path.Combine(DataDirectory, "settings.json");



        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new AppSettings();
                }

                var json = File.ReadAllText(SettingsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AppSettings();
                }

                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(DataDirectory);
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Best-effort write; ignore failures.
            }
        }
    }
}
