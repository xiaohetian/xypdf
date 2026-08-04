using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;
using System.Windows;

namespace PdfToolbox.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _appName = "PDF全能工具箱";

    [ObservableProperty]
    private string _version = "";

    [ObservableProperty]
    private string _author = "小河田";

    [ObservableProperty]
    private string _weChat = "DoAfterThink";

    [ObservableProperty]
    private string _description = "一款功能强大的桌面PDF处理工具，支持PDF阅读、合并、拆分、裁剪、水印、加密解密、图片转PDF、文档转PDF等多种功能。基于 .NET 8 + WPF 构建，采用现代化MVVM架构，界面美观，操作简便，无需安装GhostScript等外部依赖。";

    [ObservableProperty]
    private string _copyright = "© 2026 小河田";

    public AboutViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ver = assembly.GetName().Version;
        Version = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
    }

    [RelayCommand]
    private void OpenWeChat()
    {
        try
        {
            Clipboard.SetText(WeChat);
        }
        catch
        {
            MessageBox.Show("无法访问剪贴板", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
