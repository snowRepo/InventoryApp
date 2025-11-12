using System;
using System.IO;
using System.Text.Json;

namespace InventoryApp.Services
{
    public class UserSettings
    {
        public string Currency { get; set; } = "USD";
        public string CurrencySymbol { get; set; } = "$";
    }

    public class UserSettingsService
    {
        public event EventHandler SettingsChanged;

        private const string SettingsFileName = "usersettings.json";
        private UserSettings _currentSettings;

        public UserSettingsService()
        {
            _currentSettings = LoadSettings() ?? new UserSettings();
        }

        public UserSettings CurrentSettings => _currentSettings;

        public void UpdateCurrency(string currencyCode)
        {
            _currentSettings.Currency = currencyCode;
            _currentSettings.CurrencySymbol = GetCurrencySymbol(currencyCode);
            SaveSettings(_currentSettings);
        }

        private string GetCurrencySymbol(string currencyCode)
        {
            return currencyCode switch
            {
                "USD" => "$",
                "GBP" => "£",
                "CNY" => "¥",
                "GHS" => "GH₵",
                "NGN" => "₦",
                _ => "$"
            };
        }

        public void SaveSettings(UserSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var filePath = GetSettingsPath();
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
            _currentSettings = settings;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private UserSettings LoadSettings()
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<UserSettings>(json);
                }
                catch
                {
                    return new UserSettings();
                }
            }
            return new UserSettings();
        }

        private string GetSettingsPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "InventoryApp");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }
    }
}
