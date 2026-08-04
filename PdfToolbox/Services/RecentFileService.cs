using PdfToolbox.Models;

namespace PdfToolbox.Services;

public class RecentFileService
{
    private const int MaxRecentFiles = 10;

    private static readonly Lazy<RecentFileService> _instance = new(() => new());
    public static RecentFileService Instance => _instance.Value;

    private readonly SettingsService _settings = SettingsService.Instance;

    public List<RecentFileInfo> GetFiles()
    {
        var paths = _settings.Settings.AppSettingsData.RecentFiles;
        return paths.Select(p => new RecentFileInfo { FilePath = p }).ToList();
    }

    public void AddFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        var list = _settings.Settings.AppSettingsData.RecentFiles;

        // if already exists, move to top
        list.RemoveAll(f => f.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        list.Insert(0, filePath);

        // trim to max
        while (list.Count > MaxRecentFiles)
            list.RemoveAt(list.Count - 1);

        _settings.Save();
    }

    public void Clear()
    {
        _settings.Settings.AppSettingsData.RecentFiles.Clear();
        _settings.Save();
    }
}
