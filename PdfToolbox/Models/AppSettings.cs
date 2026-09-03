using Newtonsoft.Json;
using System.IO;

namespace PdfToolbox.Models;

/// <summary>
/// 应用程序设置模型，对应 appsettings.json 中的 "AppSettings" 对象。
/// </summary>
public class AppSettingsData
{
    [JsonProperty("DefaultOutputPath")]
    public string DefaultOutputPath { get; set; } = "";

    [JsonProperty("DefaultDpi")]
    public int DefaultDpi { get; set; } = 150;

    [JsonProperty("DefaultPageSize")]
    public string DefaultPageSize { get; set; } = "A4";

    [JsonProperty("DefaultMargin")]
    public int DefaultMargin { get; set; } = 20;

    [JsonProperty("SelectedTheme")]
    public string SelectedTheme { get; set; } = "默认蓝";

    [JsonProperty("AutoSave")]
    public bool AutoSave { get; set; } = true;

    [JsonProperty("RecentFiles")]
    public List<string> RecentFiles { get; set; } = new();
}

/// <summary>
/// 顶层配置容器，对应 appsettings.json 根对象。
/// </summary>
public class AppSettings
{
    [JsonProperty("AppSettings")]
    public AppSettingsData Settings { get; set; } = new();

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

    /// <summary>便捷访问底层数据</summary>
    public AppSettingsData Data => Settings;
}
