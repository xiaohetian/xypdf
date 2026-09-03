using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;
using System.IO;

namespace PdfToolbox.ViewModels;

public partial class PdfRotateViewModel : ViewModelBase
{
    private readonly PdfToolService _pdfToolService = new();

    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private int _rotationAngle = 90;
    [ObservableProperty] private bool _useAllPages = true;
    [ObservableProperty] private string _pageRange = "";

    public PdfRotateViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "rotated.pdf");
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "选择PDF文件" };
        if (dialog.ShowDialog() == true)
        {
            InputPath = dialog.FileName;
            TotalPages = _pdfToolService.GetPdfPageCount(InputPath);
            OutputPath = Path.Combine(
                Path.GetDirectoryName(InputPath) ?? "",
                Path.GetFileNameWithoutExtension(InputPath) + "_旋转.pdf");
        }
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "保存PDF", FileName = Path.GetFileName(OutputPath) };
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ProcessAsync()
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            System.Windows.MessageBox.Show("请选择输入文件", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出路径", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsProcessing = true; ProgressValue = 0; IsBusy = true;
        StatusMessage = "正在旋转页面...";

        using var cts = new CancellationTokenSource();
        var progress = new Progress<int>(v => ProgressValue = v);

        try
        {
            var pageRange = UseAllPages ? "all" : PageRange;
            var result = await _pdfToolService.RotatePagesAsync(InputPath, OutputPath, pageRange, RotationAngle, progress, cts.Token);

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show("旋转成功!", "完成", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                StatusMessage = "处理完成";
            }
            else
            {
                System.Windows.MessageBox.Show(result, "失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "处理失败";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"处理失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally { IsProcessing = false; IsBusy = false; }
    }
}
