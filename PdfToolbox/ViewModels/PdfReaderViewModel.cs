using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Models;
using PdfToolbox.Services;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace PdfToolbox.ViewModels;

public partial class PdfReaderViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<PdfTabItem> _tabs = new();

    [ObservableProperty]
    private PdfTabItem? _selectedTab;

    [ObservableProperty]
    private BitmapSource? _currentPageImage;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private ObservableCollection<PageImageItem> _allPages = new();

    [ObservableProperty]
    private bool _showAllPages = true;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private string _currentFileName = "";

    [ObservableProperty]
    private bool _isDocumentLoaded;

    [ObservableProperty]
    private ObservableCollection<RecentFileInfo> _recentFileItems = new();

    [ObservableProperty]
    private RecentFileInfo? _selectedRecentFile;

    [ObservableProperty]
    private int _currentRotation = 0;

    public PdfReaderViewModel()
    {
        RefreshRecentFiles();
    }

    public void RefreshRecentFiles()
    {
        RecentFileItems.Clear();
        foreach (var f in RecentFileService.Instance.GetFiles())
            RecentFileItems.Add(f);
    }

    partial void OnSelectedRecentFileChanged(RecentFileInfo? value)
    {
        if (value != null)
        {
            AddTabInternal(value.FilePath);
            SelectedRecentFile = null;
        }
    }

    [RelayCommand]
    private void OpenPdf()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "打开PDF文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
                AddTab(filePath);
        }
    }

    private void AddTab(string filePath) => AddTabInternal(filePath);

    public void AddPdfs(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        foreach (var path in filePaths)
            AddTabInternal(path);
    }

    private async void AddTabInternal(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        var tab = new PdfTabItem
        {
            FilePath = filePath,
            FileName = fileName,
            IsSelected = true,
            RenderService = new PdfRenderService()
        };

        var (loadOk, loadError) = await tab.RenderService.LoadDocumentAsync(filePath);
        if (loadOk)
        {
            tab.PageCount = tab.RenderService.PageCount;
            tab.CurrentPage = 1;
            tab.IsLoaded = true;
            RecentFileService.Instance.AddFile(filePath);
            RefreshRecentFiles();
        }
        else
        {
            var message = $"无法打开文件: {filePath}";
            if (!string.IsNullOrEmpty(loadError))
                message += $"\n\n错误详情: {loadError}";
            System.Windows.MessageBox.Show(message, "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            tab.IsSelected = false;
            return;
        }

        var existingTab = Tabs.FirstOrDefault(t => t.FilePath == filePath);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        Tabs.Add(tab);
        SelectedTab = tab;
        await LoadCurrentPageAsync();
    }

    partial void OnSelectedTabChanged(PdfTabItem? value)
    {
        if (value != null && value.IsLoaded && value.RenderService != null)
        {
            TotalPages = value.RenderService.PageCount;
            CurrentPage = value.CurrentPage;
            CurrentFileName = value.FileName;
            IsDocumentLoaded = true;
            _ = LoadCurrentPageAsync();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; _ = LoadCurrentPageAsync(); }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1) { CurrentPage--; _ = LoadCurrentPageAsync(); }
    }

    [RelayCommand]
    private void FirstPage() { CurrentPage = 1; _ = LoadCurrentPageAsync(); }

    [RelayCommand]
    private void LastPage() { CurrentPage = TotalPages; _ = LoadCurrentPageAsync(); }

    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < 3.0) { ZoomLevel += 0.25; _ = LoadCurrentPageAsync(); }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > 0.25) { ZoomLevel -= 0.25; _ = LoadCurrentPageAsync(); }
    }

    [RelayCommand]
    private void FitWidth() { ZoomLevel = 1.0; _ = LoadCurrentPageAsync(); }

    [RelayCommand]
    private void RotateLeft()
    {
        CurrentRotation -= 90;
        if (CurrentRotation < 0) CurrentRotation += 360;
        _ = LoadCurrentPageAsync();
    }

    [RelayCommand]
    private void RotateRight()
    {
        CurrentRotation += 90;
        if (CurrentRotation >= 360) CurrentRotation -= 360;
        _ = LoadCurrentPageAsync();
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        ShowAllPages = !ShowAllPages;
        _ = LoadCurrentPageAsync();
    }

    private async Task LoadCurrentPageAsync()
    {
        if (SelectedTab == null || SelectedTab.RenderService == null || SelectedTab.RenderService.PageCount == 0) return;

        if (ShowAllPages)
        {
            AllPages.Clear();
            for (int i = 0; i < SelectedTab.RenderService.PageCount; i++)
            {
                if (App.ShutdownCts.IsCancellationRequested) return;
                var image = await SelectedTab.RenderService.RenderPageAsync(i, ZoomLevel);
                if (image != null)
                {
                    if (CurrentRotation != 0)
                        image = new TransformedBitmap(image, new System.Windows.Media.RotateTransform(CurrentRotation));
                    AllPages.Add(new PageImageItem { PageNumber = i + 1, Image = image });
                }
            }
        }
        else
        {
            var image = await SelectedTab.RenderService.RenderPageAsync(CurrentPage - 1, ZoomLevel);
            if (CurrentRotation != 0 && image != null)
                CurrentPageImage = new TransformedBitmap(image, new System.Windows.Media.RotateTransform(CurrentRotation));
            else
                CurrentPageImage = image;
        }
    }

    [RelayCommand]
    private void CloseTab(PdfTabItem? tab)
    {
        if (tab == null) return;
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        tab.RenderService?.CloseDocument();

        if (Tabs.Count > 0)
            SelectedTab = Tabs[Math.Max(0, index - 1)];
        else
        {
            CurrentPageImage = null;
            AllPages.Clear();
            TotalPages = 0;
            CurrentPage = 1;
            CurrentFileName = "";
            IsDocumentLoaded = false;
        }
    }

    public override void OnDeactivated()
    {
        foreach (var tab in Tabs)
            tab.RenderService?.CloseDocument();
        base.OnDeactivated();
    }
}

public partial class PdfTabItem : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private int _pageCount;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private PdfRenderService? _renderService;
}

public partial class PageImageItem : ObservableObject
{
    [ObservableProperty] private int _pageNumber;
    [ObservableProperty] private BitmapSource? _image;
}
