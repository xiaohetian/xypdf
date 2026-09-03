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

    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private string _fileNameTemplate = "{0}";
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isSplitting;
    [ObservableProperty] private int _selectedSplitMode;
    [ObservableProperty] private string _pageRange = "1-5";
    [ObservableProperty] private string _extractedPages = "1,3,5";

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
        if (dialog.ShowDialog() == true) SetInputFile(dialog.FileName);
    }

    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
        InputPath = filePath;
        try { TotalPages = _pdfService.GetPdfPageCount(InputPath); }
        catch { TotalPages = 0; }
        FileNameTemplate = System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_第{0}页";
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var folder = FolderBrowserHelper.SelectFolder("选择输出目录");
        if (!string.IsNullOrEmpty(folder)) OutputPath = folder;
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
        string? result = null;

        try
        {
            using var cts = new System.Threading.CancellationTokenSource();
            var progress = new Progress<int>(value => ProgressValue = value);

            switch (SelectedSplitMode)
            {
                case 0:
                    result = await _pdfService.SplitPdfByRangeAsync(
                        InputPath, OutputPath, FileNameTemplate + ".pdf", PageRange, progress, cts.Token);
                    break;
                case 1:
                    result = await _pdfService.SplitPdfToSinglePagesAsync(
                        InputPath, OutputPath, FileNameTemplate, progress, cts.Token);
                    break;
                case 2:
                {
                    var pages = ExtractedPages.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out int p) ? p : -1)
                        .Where(p => p > 0 && p <= TotalPages).ToList();
                    if (pages.Count == 0)
                    {
                        System.Windows.MessageBox.Show("请输入有效的页码", "提示",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsSplitting = false; return;
                    }
                    var outputFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_extracted.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, outputFile, pages, progress, cts.Token);
                    break;
                }
                case 3:
                {
                    var oddPages = Enumerable.Range(1, TotalPages).Where(p => p % 2 != 0).ToList();
                    var oddFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_奇数页.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, oddFile, oddPages, progress, cts.Token);
                    break;
                }
                case 4:
                {
                    var evenPages = Enumerable.Range(1, TotalPages).Where(p => p % 2 == 0).ToList();
                    var evenFile = System.IO.Path.Combine(OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_偶数页.pdf");
                    result = await _pdfService.ExtractPagesAsync(InputPath, evenFile, evenPages, progress, cts.Token);
                    break;
                }
            }
        }
        catch (System.OperationCanceledException)
        {
            StatusMessage = "拆分已取消";
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"拆分失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "拆分失败";
        }
        finally
        {
            IsSplitting = false;
            IsBusy = false;
        }

        if (!IsBusy)
        {
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
    }

    [RelayCommand]
    private void CancelSplit() { }
}
