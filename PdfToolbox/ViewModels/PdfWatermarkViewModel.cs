using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using PdfToolbox.Helpers;

namespace PdfToolbox.ViewModels;

public partial class PdfWatermarkViewModel : ViewModelBase
{
    private readonly PdfToolService _pdfToolService = new();

    // ===== 工作模式 =====
    [ObservableProperty] private int _workMode; // 0=添加水印, 1=去除水印

    // ===== 公共属性 =====
    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isProcessing;

    // ===== 添加水印参数 =====
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

    // ===== 去除水印参数 =====
    [ObservableProperty] private string _removeColor = "#C0C0C0";
    [ObservableProperty] private int _removeTolerance = 100;
    [ObservableProperty] private double _removeStrength = 1.0;

    public PdfWatermarkViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "watermarked.pdf");
    }

    partial void OnWorkModeChanged(int value)
    {
        // 切换模式时自动设置输出文件名
        if (!string.IsNullOrEmpty(InputPath))
        {
            if (value == 0)
                OutputPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(InputPath) ?? "",
                    System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_水印.pdf");
            else
                OutputPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(InputPath) ?? "",
                    System.IO.Path.GetFileNameWithoutExtension(InputPath) + "_去水印.pdf");
        }
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "选择PDF文件" };
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
            TotalPages = _pdfToolService.GetPdfPageCount(InputPath);
        }
        catch
        {
            TotalPages = 0;
        }

        var dir = System.IO.Path.GetDirectoryName(InputPath) ?? "";
        var name = System.IO.Path.GetFileNameWithoutExtension(InputPath);
        var suffix = WorkMode == 0 ? "_水印" : "_去水印";
        OutputPath = System.IO.Path.Combine(dir, name + suffix + ".pdf");
    }

    [RelayCommand]
    private void SelectWatermarkImage()
    {
        var dialog = new OpenFileDialog { Filter = "图片文件 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg", Title = "选择水印图片" };
        if (dialog.ShowDialog() == true)
        {
            WatermarkImagePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog { Filter = "PDF文件 (*.pdf)|*.pdf", Title = "保存PDF", FileName = System.IO.Path.GetFileName(OutputPath) };
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
        StatusMessage = "正在处理...";

        using var cts = new CancellationTokenSource();
        var progress = new Progress<int>(v => ProgressValue = v);

        try
        {
            string? result;

            if (WorkMode == 0)
            {
                // === 添加水印 ===
                if (WatermarkType == 0)
                {
                    if (string.IsNullOrWhiteSpace(WatermarkText))
                    {
                        System.Windows.MessageBox.Show("请输入水印文字", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsProcessing = false; IsBusy = false; return;
                    }
                    StatusMessage = "正在添加文字水印...";
                    result = await _pdfToolService.AddTextWatermarkAsync(InputPath, OutputPath,
                        WatermarkText, FontSize, WatermarkColor, Opacity, Angle, Tile, progress, cts.Token);
                }
                else
                {
                    if (string.IsNullOrEmpty(WatermarkImagePath))
                    {
                        System.Windows.MessageBox.Show("请选择水印图片", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        IsProcessing = false; IsBusy = false; return;
                    }
                    StatusMessage = "正在添加图片水印...";
                    result = await _pdfToolService.AddImageWatermarkAsync(InputPath, OutputPath,
                        WatermarkImagePath, ImageWidth, ImageHeight, Opacity, Tile, progress, cts.Token);
                }
            }
            else
            {
                // === 去除水印 ===
                StatusMessage = "正在去除水印...";
                result = await _pdfToolService.RemoveWatermarkAsync(InputPath, OutputPath,
                    RemoveColor, RemoveTolerance, RemoveStrength, progress, cts.Token);
            }

            if (string.IsNullOrEmpty(result))
            {
                System.Windows.MessageBox.Show(
                    WorkMode == 0 ? "水印添加成功!" : "水印去除成功!",
                    "完成", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
