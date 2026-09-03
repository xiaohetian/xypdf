using QuestPDF.Infrastructure;
using PdfSharp.Fonts;
using PdfToolbox.Helpers;
using PdfToolbox.Services;

namespace PdfToolbox;

public partial class App : System.Windows.Application
{
    public App()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        GlobalFontSettings.FontResolver = new SimpleFontResolver();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var savedTheme = SettingsService.Instance.Settings.SelectedTheme;
        ThemeService.Instance.ApplyTheme(string.IsNullOrEmpty(savedTheme) ? "默认蓝" : savedTheme);
    }
}
