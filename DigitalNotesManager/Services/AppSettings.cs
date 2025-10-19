using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace DigitalNotesManager.Services
{
    public class AppSettings
    {
        public string EditorFontFamily { get; set; } = "Segoe UI";
        public float EditorFontSize { get; set; } = 10f;
        public bool DefaultReminderChecked { get; set; } = false;
        public List<string> DefaultCategories { get; set; } = new() { "General", "Personal", "Work", "Study", "Ideas" };


        public static string GetSettingsDir(string username)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DigitalNotesManager", username ?? "Default");
            Directory.CreateDirectory(dir);
            return dir;
        }


        public static string GetSettingsPath(string username)
        => Path.Combine(GetSettingsDir(username), "settings.json");


        public static AppSettings Load(string username)
        {
            try
            {
                var path = GetSettingsPath(username);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var obj = JsonSerializer.Deserialize<AppSettings>(json);
                    if (obj != null) return obj;
                }
            }
            catch { }
            return new AppSettings();
        }


        public void Save(string username)
        {
            try
            {
                var path = GetSettingsPath(username);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}