using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolbox.Helpers;
using PdfToolbox.Services;

namespace PdfToolbox.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = SettingsService.Instance;

    public string[] Themes { get; } = ["默认蓝", "翡翠绿", "典雅紫", "暗色"];

    [ObservableProperty]
    private string _selectedTheme = "默认蓝";

    [ObservableProperty]
    private string _defaultOutputPath = "";

    [ObservableProperty]
    private int _defaultDpi = 150;

    [ObservableProperty]
    private bool _autoSave = true;

    public int ThemeIndex
    {
        get
        {
            var idx = Array.IndexOf(Themes, SelectedTheme);
            return idx >= 0 ? idx : 0;
        }
        set
        {
            if (value >= 0 && value < Themes.Length)
                SelectedTheme = Themes[value];
        }
    }

    public SettingsViewModel() => LoadSettings();

    private void LoadSettings()
    {
        var data = _settingsService.Settings;
        SelectedTheme = string.IsNullOrEmpty(data.SelectedTheme) ? Themes[0] : data.SelectedTheme;
        DefaultOutputPath = _settingsService.GetDefaultOutputPath();
        DefaultDpi = data.DefaultDpi;
        AutoSave = data.AutoSave;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        OnPropertyChanged(nameof(ThemeIndex));
        ThemeService.Instance.ApplyTheme(value);
        _settingsService.Settings.SelectedTheme = value;
        if (AutoSave) _settingsService.Save();
    }

    partial void OnDefaultOutputPathChanged(string value)
    {
        _settingsService.Settings.DefaultOutputPath = value;
        if (AutoSave) _settingsService.Save();
    }

    partial void OnDefaultDpiChanged(int value)
    {
        _settingsService.Settings.DefaultDpi = value;
        if (AutoSave) _settingsService.Save();
    }

    partial void OnAutoSaveChanged(bool value)
    {
        _settingsService.Settings.AutoSave = value;
        _settingsService.Save();
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var path = FolderBrowserHelper.SelectFolder("选择默认输出目录");
        if (path != null) DefaultOutputPath = path;
    }
}
