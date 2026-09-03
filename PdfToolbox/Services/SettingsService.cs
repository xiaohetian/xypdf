using PdfToolbox.Models;

namespace PdfToolbox.Services;

public class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private readonly AppSettings _settings;

    public AppSettingsData Settings => _settings.Settings;

    private SettingsService()
    {
        _settings = AppSettings.Load();
    }

    public void Save() => _settings.Save();

    public string GetDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(_settings.Settings.DefaultOutputPath))
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return _settings.Settings.DefaultOutputPath;
    }

    public void SetDefaultOutputPath(string path)
    {
        _settings.Settings.DefaultOutputPath = path;
        Save();
    }
}
