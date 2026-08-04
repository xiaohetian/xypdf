using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

public partial class PdfSplitViewModel : ViewModelBase
{
    private readonly PdfService _pdfService = new();

    [ObservableProperty]
    private string _inputPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _fileNameTemplate = "{0}";

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isSplitting;

    [ObservableProperty]
    private int _selectedSplitMode;

    [ObservableProperty]
    private string _pageRange = "1-5";

    [ObservableProperty]
    private string _extractedPages = "1,3,5";

    private CancellationTokenSource? _cancellationTokenSource;

    public PdfSplitViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "split");
        System.IO.Directory.CreateDirectory(OutputPath);
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择要拆分的PDF文件"
        };

        if (dialog.ShowDialog() == true)
        {
            SetInputFile(dialog.FileName);
        }
    }

    /// <summary>
    /// 设置输入 PDF 文件（公开方法，用于拖拽支持）。
    /// </summary>
    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return;
        }

        InputPath = filePath;
        try
        {
            TotalPages = _pdfService.GetPdfPageCount(InputPath);
        }
        catch
        {
            TotalPages = 0;
        }
        FileNameTemplate = System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_第{0}页";
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var folder = FolderBrowserHelper.SelectFolder("选择输出目录");
        if (!string.IsNullOrEmpty(folder))
        {
            OutputPath = folder;
        }
    }

    [RelayCommand]
    private async Task SplitAsync()
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            System.Windows.MessageBox.Show("请选择输入文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出目录", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsSplitting = true;
        ProgressValue = 0;
        IsBusy = true;

        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<int>(value => ProgressValue = value);

        try
        {
            string? result = null;

            switch (SelectedSplitMode)
            {
                case 0:
                    StatusMessage = "正在按范围拆分...";
                    result = await _pdfService.SplitPdfByRangeAsync(
                        InputPath, OutputPath, FileNameTemplate + ".pdf", PageRange, progress, _cancellationTokenSource.Token);
                    break;

                case 1:
                    StatusMessage = "正在拆分为单页...";
                    result = await _pdfService.SplitPdfToSinglePagesAsync(
                        InputPath, OutputPath, FileNameTemplate, progress, _cancellationTokenSource.Token);
                    break;

                case 2:
                    StatusMessage = "正在提取指定页...";
                    var pages = ExtractedPages.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out int p) ? p : -1)
                        .Where(p => p > 0 && p <= TotalPages)
                        .ToList();

                    if (pages.Count == 0)
                    {
                        System.Windows.MessageBox.Show("请输入有效的页码", "提示",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsSplitting = false;
                        IsBusy = false;
                        return;
                    }

                    var outputFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_extracted.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, outputFile, pages, progress, _cancellationTokenSource.Token);
                    break;

                case 3:
                    StatusMessage = "正在提取奇数页...";
                    var oddPages = Enumerable.Range(1, TotalPages).Where(p => p % 2 != 0).ToList();
                    var oddOutputFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_奇数页.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, oddOutputFile, oddPages, progress, _cancellationTokenSource.Token);
                    break;

                case 4:
                    StatusMessage = "正在提取偶数页...";
                    var evenPages = Enumerable.Range(1, TotalPages).Where(p => p % 2 == 0).ToList();
                    var evenOutputFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_偶数页.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, evenOutputFile, evenPages, progress, _cancellationTokenSource.Token);
                    break;
            }

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show("拆分完成!", "完成",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = "拆分完成";
            }
            else
            {
                System.Windows.MessageBox.Show(result, "拆分失败",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "拆分失败";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "拆分已取消";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"拆分失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "拆分失败";
        }
        finally
        {
            IsSplitting = false;
            IsBusy = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelSplit()
    {
        _cancellationTokenSource?.Cancel();
    }
}