using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Helpers;
using PdfToolbox.Services;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PdfToolbox.ViewModels;

public partial class PdfToImageViewModel : ViewModelBase
{
    private readonly PdfRenderService _renderService = new();

    [ObservableProperty]
    private ObservableCollection<PdfFileItem> _pdfFiles = new();

    [ObservableProperty]
    private PdfFileItem? _selectedFile;

    [ObservableProperty]
    private string _outputFolder = "";

    [ObservableProperty]
    private string _imageFormat = "PNG";

    [ObservableProperty]
    private int _dpi = 150;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isConverting;

    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<string> ImageFormats { get; } = new()
    {
        "PNG", "JPG", "BMP", "TIFF"
    };

    public ObservableCollection<int> DpiOptions { get; } = new()
    {
        96, 150, 200, 300, 600
    };

    public ICommand ConvertAsyncCommand { get; }
    public ICommand CancelConvertCommand { get; }

    public PdfToImageViewModel()
    {
        OutputFolder = SettingsService.Instance.GetDefaultOutputPath();
        ConvertAsyncCommand = new AsyncRelayCommand(ConvertAsyncImpl);
        CancelConvertCommand = new RelayCommand(CancelConvertImpl);
    }

    [RelayCommand]
    private void AddPdfs()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择PDF文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            AddPdfFiles(dialog.FileNames);
        }
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = Helpers.FolderBrowserHelper.SelectFolder("选择包含PDF文件的文件夹");
        if (string.IsNullOrEmpty(path)) return;

        var files = System.IO.Directory.EnumerateFiles(path, "*.pdf", System.IO.SearchOption.AllDirectories);
        AddPdfFiles(files);
    }

    /// <summary>
    /// 公开方法：添加多个 PDF 文件到转换列表（用于拖拽支持）。
    /// 自动跳过重复文件。
    /// </summary>
    public void AddPdfFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;

        int added = 0, skipped = 0;
        foreach (var file in filePaths)
        {
            if (!System.IO.Path.GetExtension(file).Equals(".pdf", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (PdfFiles.Any(p => string.Equals(p.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }

            PdfFiles.Add(new PdfFileItem
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file)
            });
            added++;
        }

        if (added > 0 || skipped > 0)
        {
            var messages = new List<string>();
            if (added > 0) messages.Add($"已添加 {added} 个文件");
            if (skipped > 0) messages.Add($"跳过 {skipped} 个重复");
            StatusMessage = string.Join("，", messages);
        }
    }

    [RelayCommand]
    private void RemovePdf(PdfFileItem? item)
    {
        if (item != null)
        {
            PdfFiles.Remove(item);
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        PdfFiles.Clear();
    }

    [RelayCommand]
    private void SelectOutputFolder()
    {
        var folder = FolderBrowserHelper.SelectFolder("选择输出文件夹");
        if (!string.IsNullOrEmpty(folder))
        {
            OutputFolder = folder;
        }
    }

    private async Task ConvertAsyncImpl()
    {
        // ========== 调试测试：验证按钮点击确实触达 ==========
        IsConverting = true;
        ProgressValue = 0;

        // 先做简单的进度测试
        for (int i = 0; i <= 100; i += 10)
        {
            await Task.Delay(200);
            ProgressValue = i;
        }

        System.Windows.MessageBox.Show("点击测试完成，下面开始真正的转换", "调试",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        // =================================================

        if (PdfFiles.Count == 0)
        {
            System.Windows.MessageBox.Show("请先添加PDF文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            IsConverting = false;
            IsBusy = false;
            return;
        }

        if (string.IsNullOrEmpty(OutputFolder))
        {
            System.Windows.MessageBox.Show("请选择输出文件夹", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            IsConverting = false;
            IsBusy = false;
            return;
        }

        ProgressValue = 0;
        IsBusy = true;
        StatusMessage = "正在转换...";

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var filePaths = new List<string>();
        foreach (var f in PdfFiles)
            filePaths.Add(f.FilePath);

        var outputFolder = OutputFolder;
        var imageFormat = ImageFormat;
        var dpi = Dpi;

        var progress = new Progress<int>(value => ProgressValue = value);

        int totalPages = 0;
        int processedPages = 0;
        string? fatalError = null;

        try
        {
            await Task.Run(() =>
            {
                foreach (var filePath in filePaths)
                {
                    if (token.IsCancellationRequested) return;

                    string? error = null;
                    if (!_renderService.LoadDocument(filePath, out error))
                    {
                        System.Diagnostics.Debug.WriteLine($"无法打开PDF: {filePath} - {error}");
                        continue;
                    }

                    totalPages += _renderService.PageCount;
                    _renderService.CloseDocument();
                }

                if (totalPages == 0)
                {
                    fatalError = "无法读取任何PDF文件的页面信息，请检查PDF文件是否损坏或格式不正确";
                    return;
                }

                foreach (var filePath in filePaths)
                {
                    if (token.IsCancellationRequested) return;

                    string? error = null;
                    if (!_renderService.LoadDocument(filePath, out error))
                    {
                        System.Diagnostics.Debug.WriteLine($"无法加载PDF: {filePath} - {error}");
                        continue;
                    }

                    string baseName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_page";

                    for (int pageIndex = 0; pageIndex < _renderService.PageCount; pageIndex++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            _renderService.CloseDocument();
                            return;
                        }

                        using (var bitmap = _renderService.RenderPageAsBitmap(pageIndex, dpi))
                        {
                            if (bitmap != null)
                            {
                                string imageFilePath = System.IO.Path.Combine(
                                    outputFolder,
                                    $"{baseName}{pageIndex + 1}.{imageFormat.ToLower()}");

                                switch (imageFormat.ToUpper())
                                {
                                    case "PNG":
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Png);
                                        break;
                                    case "JPG":
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        break;
                                    case "BMP":
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Bmp);
                                        break;
                                    case "TIFF":
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Tiff);
                                        break;
                                    default:
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Png);
                                        break;
                                }
                            }
                        }

                        processedPages++;
                        int percent = (int)((double)processedPages / totalPages * 100);
                        ((IProgress<int>)progress).Report(percent);
                    }

                    _renderService.CloseDocument();
                }
            }, token);

            if (fatalError != null)
            {
                System.Windows.MessageBox.Show(fatalError, "转换失败",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "转换失败";
            }
            else if (!token.IsCancellationRequested)
            {
                System.Windows.MessageBox.Show($"转换完成!\n共转换 {processedPages} 张图片\n输出目录: {outputFolder}", "完成",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = $"转换完成，共 {processedPages} 张图片";
            }
            else
            {
                StatusMessage = "转换已取消";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "转换已取消";
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"转换失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "转换失败";
        }
        finally
        {
            IsConverting = false;
            IsBusy = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelConvertImpl()
    {
        _cancellationTokenSource?.Cancel();
    }
}
