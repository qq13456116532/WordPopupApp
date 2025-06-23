using Newtonsoft.Json;
using System;
using System.IO;
using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            // 将配置文件保存在 AppData 目录下
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appDataPath, "WordPopupApp");
            Directory.CreateDirectory(appDir);
            _settingsFilePath = Path.Combine(appDir, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json);
            }
            return new AppSettings(); // 返回默认设置
        }

        public void SaveSettings(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }
    }
}