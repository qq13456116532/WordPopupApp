using Newtonsoft.Json;
using System.IO;

using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class SettingsService
    {
        private readonly string settingsFile = "settings.json";
        public AppSettings LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                return settings;
            }
            return new AppSettings
            {
            };
        }

        public void SaveSettings(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFile, json);
        }
    }

}