using System;
using System.IO;
using System.Text.Json;
using DVVdownloader.Models;

namespace DVVdownloader.Services
{
    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;

        private readonly string _settingsPath;
        public AppSettings CurrentSettings { get; private set; }

        private SettingsService()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DVVdownloader"
            );
            Directory.CreateDirectory(appData);
            _settingsPath = Path.Combine(appData, "settings.json");
            CurrentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Ayarlar yüklenirken hata oluştu: {ex.Message}", "SettingsService");
            }

            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                CurrentSettings = settings;
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                LogService.Instance.Info("Uygulama ayarları başarıyla kaydedildi.", "SettingsService");
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "SettingsService");
            }
        }
    }
}
