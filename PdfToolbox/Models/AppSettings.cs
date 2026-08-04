using Newtonsoft.Json;
using System.IO;

namespace PdfToolbox.Models;

public class AppSettings
{
    public AppSettingsData AppSettingsData { get; set; } = new();

    private static readonly string ConfigFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
        }
    }
}

public class AppSettingsData
{
    public string DefaultOutputPath { get; set; } = "";
    public int DefaultDpi { get; set; } = 150;
    public string DefaultPageSize { get; set; } = "A4";
    public int DefaultMargin { get; set; } = 20;
    public string SelectedTheme { get; set; } = "默认蓝";
    public bool AutoSave { get; set; } = true;
    public List<string> RecentFiles { get; set; } = new();
}