using Newtonsoft.Json;
using System.IO;

using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class SettingsService
    {
        private readonly string settingsFile = "settings.json";
        private readonly string defaultTemplate = @"
    <div style='font-family:Microsoft YaHei,Arial,sans-serif;font-size:18px;line-height:1.5;padding:10px;background:#f6f8fa;border-radius:10px;'>
        <div style='color:#222;font-weight:bold;font-size:22px;'>{{Word}}</div>
        <div style='margin:10px 0 6px 0;color:#0066cc;'>{{Phonetic}}</div>
        <div style='color:#444;'>{{Definition}}</div>
        <div style='margin-top:12px;color:#aaa;font-size:14px;'>{{Example}}</div>
    </div>
    ";
        public AppSettings LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                if (string.IsNullOrEmpty(settings.CardTemplate))
                    settings.CardTemplate = defaultTemplate;
                return settings;
            }
            return new AppSettings
            {
                CardTemplate = defaultTemplate
            };
        }

        public void SaveSettings(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFile, json);
        }
    }

}