using QuestPDF.Infrastructure;
using PdfSharp.Fonts;
using PdfToolbox.Helpers;
using PdfToolbox.Services;

namespace PdfToolbox;

public partial class App : System.Windows.Application
{
    /// <summary>全局取消令牌：窗口关闭/应用退出时取消所有在途后台任务（如PDF渲染循环）。</summary>
    public static System.Threading.CancellationTokenSource ShutdownCts { get; } = new();

    public App()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        GlobalFontSettings.FontResolver = new SimpleFontResolver();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        ShutdownCts.Cancel();
        base.OnExit(e);
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var savedTheme = SettingsService.Instance.Settings.SelectedTheme;
        ThemeService.Instance.ApplyTheme(string.IsNullOrEmpty(savedTheme) ? "默认蓝" : savedTheme);
    }
}
