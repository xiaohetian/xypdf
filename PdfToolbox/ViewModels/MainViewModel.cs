using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using PdfToolbox.Models;
using PdfToolbox.Services;
using PdfToolbox.Views;
using System.Windows;

namespace PdfToolbox.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    private MenuItem? _selectedMenuItem;
    public MenuItem? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (value != null && value != _selectedMenuItem)
            {
                SetProperty(ref _selectedMenuItem, value);
                NavigateTo(value);
            }
        }
    }

    public ObservableCollection<MenuItem> MainMenuItems { get; } = new();
    public ObservableCollection<MenuItem> BottomMenuItems { get; } = new();
    public ObservableCollection<RecentFileInfo> RecentFileItems { get; } = new();

    private readonly Dictionary<string, Func<ViewModelBase>> _viewModelFactories = new();

    public MainViewModel()
    {
        var iconBrush = TryFindBrush("IconBrush")
            ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB0, 0xBE, 0xC5));

        MainMenuItems.Add(new() { Label = "PDF 阅读器", CommandParameter = "PDF阅读器", Icon = TryFindGeometry("IconReader"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 合并", CommandParameter = "PDF合并", Icon = TryFindGeometry("IconMerge"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 拆分", CommandParameter = "PDF拆分", Icon = TryFindGeometry("IconSplit"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 裁剪", CommandParameter = "PDF裁剪", Icon = TryFindGeometry("IconCrop"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "图片转 PDF", CommandParameter = "图片转PDF", Icon = TryFindGeometry("IconImageToPdf"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "文档转 PDF", CommandParameter = "TXT/RTF/HTML转PDF", Icon = TryFindGeometry("IconDocToPdf"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "OFD 转 PDF", CommandParameter = "OFD转PDF", Icon = TryFindGeometry("IconOfd"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 转图片", CommandParameter = "PDF转图片", Icon = TryFindGeometry("IconPdfToImage"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 水印", CommandParameter = "PDF水印", Icon = TryFindGeometry("IconWatermark"), IconColor = iconBrush });
        MainMenuItems.Add(new() { Label = "PDF 加密/解密", CommandParameter = "PDF加密/解密", Icon = TryFindGeometry("IconSecurity"), IconColor = iconBrush });

        BottomMenuItems.Add(new() { Label = "设  置", CommandParameter = "设置", Icon = TryFindGeometry("IconSettings"), IconColor = iconBrush });
        BottomMenuItems.Add(new() { Label = "关  于", CommandParameter = "关于", Icon = TryFindGeometry("IconAbout"), IconColor = iconBrush });

        RegisterViewModels();
        RefreshRecentFiles();

        if (MainMenuItems.Count > 0)
            SelectedMenuItem = MainMenuItems[0];
    }

    private void RegisterViewModels()
    {
        _viewModelFactories["PDF阅读器"] = () => new PdfReaderViewModel();
        _viewModelFactories["PDF合并"] = () => new PdfMergeViewModel();
        _viewModelFactories["PDF拆分"] = () => new PdfSplitViewModel();
        _viewModelFactories["PDF裁剪"] = () => new PdfCropViewModel();
        _viewModelFactories["图片转PDF"] = () => new ImageToPdfViewModel();
        _viewModelFactories["TXT/RTF/HTML转PDF"] = () => new DocumentToPdfViewModel();
        _viewModelFactories["OFD转PDF"] = () => new OfdConvertViewModel();
        _viewModelFactories["PDF转图片"] = () => new PdfToImageViewModel();
        _viewModelFactories["PDF水印"] = () => new PdfWatermarkViewModel();
        _viewModelFactories["PDF加密/解密"] = () => new PdfSecurityViewModel();
        _viewModelFactories["设置"] = () => new SettingsViewModel();
        _viewModelFactories["关于"] = () => new AboutViewModel();
    }

    private void NavigateTo(MenuItem menuItem)
    {
        if (CurrentViewModel != null)
            CurrentViewModel.OnDeactivated();

        if (_viewModelFactories.TryGetValue(menuItem.CommandParameter, out var factory))
        {
            var vm = factory();
            CurrentViewModel = vm;
            RefreshRecentFiles();
            vm.OnActivated();
        }
    }

    [RelayCommand]
    private void OpenRecentFile(RecentFileInfo? file)
    {
        if (file == null || !System.IO.File.Exists(file.FilePath)) return;

        // switch to PDF reader and pass the file
        var reader = new PdfReaderViewModel();
        reader.AddPdfs([file.FilePath]);
        CurrentViewModel?.OnDeactivated();
        CurrentViewModel = reader;
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        RecentFileItems.Clear();
        RecentFileService.Instance.Clear();
    }

    public void RefreshRecentFiles()
    {
        RecentFileItems.Clear();
        foreach (var f in RecentFileService.Instance.GetFiles())
            RecentFileItems.Add(f);
    }

    private static System.Windows.Media.Geometry? TryFindGeometry(string key)
    {
        try { return Application.Current?.TryFindResource(key) as System.Windows.Media.Geometry; }
        catch { return null; }
    }

    private static System.Windows.Media.Brush? TryFindBrush(string key)
    {
        try { return Application.Current?.TryFindResource(key) as System.Windows.Media.Brush; }
        catch { return null; }
    }
}
