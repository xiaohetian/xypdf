using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;

namespace PdfToolbox.ViewModels;

public partial class PdfWatermarkViewModel : ViewModelBase
{
    private readonly PdfToolService _pdfToolService = new();

    [ObservableProperty] private int _workMode;
    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isProcessing;

    // 添加水印参数
    [ObservableProperty] private int _watermarkType;
    [ObservableProperty] private string _watermarkText = "机密文件";
    [ObservableProperty] private string _watermarkImagePath = "";
    [ObservableProperty] private double _fontSize = 48;
    [ObservableProperty] private double _imageWidth = 150;
    [ObservableProperty] private double _imageHeight = 150;
    [ObservableProperty] private string _watermarkColor = "#C0C0C0";
    [ObservableProperty] private double _opacity = 0.3;
    [ObservableProperty] private double _angle = -45;
    [ObservableProperty] private bool _tile = true;

    // 去除水印参数
    [ObservableProperty] private string _removeColor = "#C0C0C0";
    [ObservableProperty] private int _removeTolerance = 100;
    [ObservableProperty] private double _removeStrength = 1.0;

    public PdfWatermarkViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "watermarked.pdf");
    }

    partial void OnWorkModeChanged(int value) { }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择PDF文件"
        };
        if (dialog.ShowDialog() == true) SetInputFile(dialog.FileName);
    }

    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
        InputPath = filePath;
        try { TotalPages = new PdfToolService().GetPdfPageCount(filePath); }
        catch { TotalPages = 0; }
    }

    [RelayCommand]
    private void SelectWatermarkImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "选择水印图片"
        };
        if (dialog.ShowDialog() == true) WatermarkImagePath = dialog.FileName;
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "保存水印PDF",
            FileName = System.IO.Path.GetFileName(OutputPath)
        };
        if (!string.IsNullOrEmpty(OutputPath))
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ProcessAsync()
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            System.Windows.MessageBox.Show("请选择输入文件", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(OutputPath))
        {
            System.Windows.MessageBox.Show("请选择输出路径", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsProcessing = true;
        string? result = null;

        try
        {
            using var cts = new System.Threading.CancellationTokenSource();
            var progress = new Progress<int>(value => ProgressValue = value);

            if (WorkMode == 0)
            {
                // 添加水印
                if (WatermarkType == 0)
                {
                    if (string.IsNullOrWhiteSpace(WatermarkText))
                    {
                        System.Windows.MessageBox.Show("请输入水印文字", "提示",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsProcessing = false; return;
                    }
                    result = await _pdfToolService.AddTextWatermarkAsync(
                        InputPath, OutputPath, WatermarkText, FontSize, WatermarkColor,
                        Opacity, Angle, Tile, progress, cts.Token);
                }
                else
                {
                    if (string.IsNullOrEmpty(WatermarkImagePath))
                    {
                        System.Windows.MessageBox.Show("请选择水印图片", "提示",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsProcessing = false; return;
                    }
                    result = await _pdfToolService.AddImageWatermarkAsync(
                        InputPath, OutputPath, WatermarkImagePath,
                        ImageWidth, ImageHeight, Opacity, Tile, progress, cts.Token);
                }
                ShowResult("水印添加", result);
            }
            else
            {
                result = await _pdfToolService.RemoveWatermarkAsync(
                    InputPath, OutputPath, RemoveColor, RemoveTolerance, RemoveStrength, progress, cts.Token);
                ShowResult("水印去除", result);
            }
        }
        catch (System.Exception ex)
        {
            IsProcessing = false;
            System.Windows.MessageBox.Show($"处理失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            StatusMessage = "处理失败";
        }
        finally { IsBusy = false; }
    }
}
