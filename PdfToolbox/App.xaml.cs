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
        ThemeService.Instance.ApplyTheme("默认蓝");
    }
}
