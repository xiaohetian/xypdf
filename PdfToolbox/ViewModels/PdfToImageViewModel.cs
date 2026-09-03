using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Helpers;
using PdfToolbox.Services;
using System.Collections.ObjectModel;
using System.Threading;
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

    public ObservableCollection<string> ImageFormats { get; } = new() { "PNG", "JPG", "BMP", "TIFF" };
    public ObservableCollection<int> DpiOptions { get; } = new() { 96, 150, 200, 300, 600 };

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
            AddPdfFiles(dialog.FileNames);
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = FolderBrowserHelper.SelectFolder("选择包含PDF文件的文件夹");
        if (string.IsNullOrEmpty(path)) return;
        var files = System.IO.Directory.EnumerateFiles(path, "*.pdf", System.IO.SearchOption.AllDirectories);
        AddPdfFiles(files);
    }

    public void AddPdfFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        int added = 0, skipped = 0;
        foreach (var file in filePaths)
        {
            if (!System.IO.Path.GetExtension(file).Equals(".pdf", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (PdfFiles.Any(p => string.Equals(p.FilePath, file, StringComparison.OrdinalIgnoreCase)))
            { skipped++; continue; }
            PdfFiles.Add(new PdfFileItem { FilePath = file, FileName = System.IO.Path.GetFileName(file) });
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
        if (item != null) PdfFiles.Remove(item);
    }

    [RelayCommand]
    private void ClearAll() => PdfFiles.Clear();

    [RelayCommand]
    private void SelectOutputFolder()
    {
        var folder = FolderBrowserHelper.SelectFolder("选择输出文件夹");
        if (!string.IsNullOrEmpty(folder)) OutputFolder = folder;
    }

    private async Task ConvertAsyncImpl()
    {
        if (PdfFiles.Count == 0)
        {
            System.Windows.MessageBox.Show("请先添加PDF文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(OutputFolder))
        {
            System.Windows.MessageBox.Show("请选择输出文件夹", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsConverting = true;
        ProgressValue = 0;
        IsBusy = true;
        StatusMessage = "正在转换...";

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var filePaths = PdfFiles.Select(f => f.FilePath).ToList();
        var outputFolder = OutputFolder;
        var imageFormat = ImageFormat;
        var dpi = Dpi;
        var progress = new Progress<int>(value => ProgressValue = value);

        int totalPages = 0;
        int processedPages = 0;
        string? fatalError = null;

        try
        {
            // Phase 1: count total pages across all files
            foreach (var filePath in filePaths)
            {
                if (token.IsCancellationRequested) return;
                var (loadOk, loadError) = await _renderService.LoadDocumentAsync(filePath);
                if (!loadOk)
                {
                    System.Diagnostics.Debug.WriteLine($"无法打开PDF: {filePath} - {loadError}");
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

            // Phase 2: render each page to image
            foreach (var filePath in filePaths)
            {
                if (token.IsCancellationRequested) return;

                var (loadOk2, loadError2) = await _renderService.LoadDocumentAsync(filePath);
                if (!loadOk2)
                {
                    System.Diagnostics.Debug.WriteLine($"无法加载PDF: {filePath} - {loadError2}");
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

                    using var bitmap = await _renderService.RenderPageAsBitmapAsync(pageIndex, dpi);
                    if (bitmap != null)
                    {
                        string imageFilePath = System.IO.Path.Combine(outputFolder,
                            $"{baseName}{pageIndex + 1}.{imageFormat.ToLower()}");
                        switch (imageFormat.ToUpper())
                        {
                            case "PNG":  bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Png);  break;
                            case "JPG":  bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Jpeg); break;
                            case "BMP":  bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Bmp);  break;
                            case "TIFF": bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Tiff); break;
                            default:     bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Png);  break;
                        }
                    }

                    processedPages++;
                    ((IProgress<int>)progress).Report((int)((double)processedPages / totalPages * 100));
                }

                _renderService.CloseDocument();
            }
        }
        catch (OperationCanceledException) { }
        catch (System.Exception ex)
        {
            fatalError = ex.Message;
        }

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

        IsConverting = false;
        IsBusy = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private void CancelConvertImpl() => _cancellationTokenSource?.Cancel();
}
public partial class PdfFileItem : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _fileName = "";
}
