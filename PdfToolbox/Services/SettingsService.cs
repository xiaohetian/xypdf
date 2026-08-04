using PdfToolbox.Models;

namespace PdfToolbox.Services;

public class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private AppSettings _settings;

    public AppSettings Settings => _settings;

    private SettingsService()
    {
        _settings = AppSettings.Load();
    }

    public void Save()
    {
        _settings.Save();
    }

    public string GetDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(_settings.AppSettingsData.DefaultOutputPath))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
        return _settings.AppSettingsData.DefaultOutputPath;
    }

    public void SetDefaultOutputPath(string path)
    {
        _settings.AppSettingsData.DefaultOutputPath = path;
        Save();
    }
}