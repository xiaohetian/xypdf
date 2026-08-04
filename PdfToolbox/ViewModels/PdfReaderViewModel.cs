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
    private readonly PdfRenderService _renderService = new();

    [ObservableProperty]
    private ObservableCollection<PdfTabItem> _tabs = new();

    [ObservableProperty]
    private PdfTabItem? _selectedTab;

    // 单页模式
    [ObservableProperty]
    private BitmapSource? _currentPageImage;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    // 全部页面模式（垂直滚动）
    [ObservableProperty]
    private ObservableCollection<PageImageItem> _allPages = new();

    [ObservableProperty]
    private bool _showAllPages = true; // 默认显示全部页面

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
            SelectedRecentFile = null; // reset so same file can be re-selected
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
            {
                AddTab(filePath);
            }
        }
    }

    private void AddTab(string filePath)
    {
        AddTabInternal(filePath);
    }

    /// <summary>
    /// 公开方法：批量添加 PDF 文件（用于拖拽支持）。
    /// 对每个文件执行与 OpenPdf 相同的逻辑。
    /// </summary>
    public void AddPdfs(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        foreach (var path in filePaths)
        {
            AddTabInternal(path);
        }
    }

    private void AddTabInternal(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        var tab = new PdfTabItem
        {
            FilePath = filePath,
            FileName = fileName,
            IsSelected = true,
            RenderService = new PdfRenderService() // 每个标签页有自己的渲染服务
        };

        string? errorMessage = null;
        if (tab.RenderService.LoadDocument(filePath, out errorMessage))
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
            if (!string.IsNullOrEmpty(errorMessage))
            {
                message += $"\n\n错误详情: {errorMessage}";
            }
            System.Windows.MessageBox.Show(message, "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        LoadCurrentPage();
    }

    partial void OnSelectedTabChanged(PdfTabItem? value)
    {
        if (value != null && value.IsLoaded && value.RenderService != null)
        {
            // 更新当前显示的文档信息，不重复加载（文档已在 AddTab 时加载）
            TotalPages = value.RenderService.PageCount;
            CurrentPage = value.CurrentPage;
            CurrentFileName = value.FileName;
            IsDocumentLoaded = true;
            LoadCurrentPage();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            LoadCurrentPage();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            LoadCurrentPage();
        }
    }

    [RelayCommand]
    private void FirstPage()
    {
        CurrentPage = 1;
        LoadCurrentPage();
    }

    [RelayCommand]
    private void LastPage()
    {
        CurrentPage = TotalPages;
        LoadCurrentPage();
    }

    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < 3.0)
        {
            ZoomLevel += 0.25;
            LoadCurrentPage();
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > 0.25)
        {
            ZoomLevel -= 0.25;
            LoadCurrentPage();
        }
    }

    [RelayCommand]
    private void FitWidth()
    {
        ZoomLevel = 1.0;
        LoadCurrentPage();
    }

    [RelayCommand]
    private void RotateLeft()
    {
        CurrentRotation -= 90;
        if (CurrentRotation < 0) CurrentRotation += 360;
        LoadCurrentPage();
    }

    [RelayCommand]
    private void RotateRight()
    {
        CurrentRotation += 90;
        if (CurrentRotation >= 360) CurrentRotation -= 360;
        LoadCurrentPage();
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        ShowAllPages = !ShowAllPages;
        LoadCurrentPage();
    }

    [ObservableProperty]
    private int _currentRotation = 0;

    private void LoadCurrentPage()
    {
        if (SelectedTab == null || SelectedTab.RenderService == null || SelectedTab.RenderService.PageCount == 0) return;

        if (ShowAllPages)
        {
            // 加载所有页面
            AllPages.Clear();
            for (int i = 0; i < SelectedTab.RenderService.PageCount; i++)
            {
                var image = SelectedTab.RenderService.RenderPage(i, ZoomLevel);
                if (image != null)
                {
                    // 应用旋转
                    if (CurrentRotation != 0)
                    {
                        var transform = new System.Windows.Media.RotateTransform(CurrentRotation);
                        image = new TransformedBitmap(image, transform);
                    }
                    AllPages.Add(new PageImageItem
                    {
                        PageNumber = i + 1,
                        Image = image
                    });
                }
            }
        }
        else
        {
            // 单页模式
            var image = SelectedTab.RenderService.RenderPage(CurrentPage - 1, ZoomLevel);
            
            // 应用旋转
            if (CurrentRotation != 0 && image != null)
            {
                var transform = new System.Windows.Media.RotateTransform(CurrentRotation);
                CurrentPageImage = new TransformedBitmap(image, transform);
            }
            else
            {
                CurrentPageImage = image;
            }
        }
    }

    [RelayCommand]
    private void CloseTab(PdfTabItem? tab)
    {
        if (tab == null) return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        // 关闭被移除标签页的渲染服务
        tab.RenderService?.CloseDocument();

        if (Tabs.Count > 0)
        {
            SelectedTab = Tabs[Math.Max(0, index - 1)];
        }
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
        // 关闭所有标签页的渲染服务
        foreach (var tab in Tabs)
        {
            tab.RenderService?.CloseDocument();
        }
        base.OnDeactivated();
    }
}

public partial class PdfTabItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _isSelected;

    // 每个标签页拥有自己的渲染服务实例
    [ObservableProperty]
    private PdfRenderService? _renderService;
}

/// <summary>
/// 页面图像项（用于全部页面视图）
/// </summary>
public partial class PageImageItem : ObservableObject
{
    [ObservableProperty]
    private int _pageNumber;

    [ObservableProperty]
    private BitmapSource? _image;
}