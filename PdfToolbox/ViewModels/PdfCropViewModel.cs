using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfToolbox.Services;
using System.Collections.ObjectModel;

namespace PdfToolbox.ViewModels;

public partial class PdfCropViewModel : ViewModelBase
{
    private readonly PdfService _pdfService = new();

    [ObservableProperty] private string _inputPath = "";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private double _leftMargin = 36;
    [ObservableProperty] private double _rightMargin = 36;
    [ObservableProperty] private double _topMargin = 36;
    [ObservableProperty] private double _bottomMargin = 36;
    [ObservableProperty] private bool _sameMarginForAll = true;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isCropping;

    public PdfCropViewModel()
    {
        OutputPath = System.IO.Path.Combine(SettingsService.Instance.GetDefaultOutputPath(), "cropped.pdf");
    }

    [RelayCommand]
    private void SelectInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择要裁剪的PDF文件"
        };
        if (dialog.ShowDialog() == true) SetInputFile(dialog.FileName);
    }

    public void SetInputFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
        InputPath = filePath;
        try { TotalPages = _pdfService.GetPdfPageCount(InputPath); }
        catch { TotalPages = 0; }
        var fileName = System.IO.Path.GetFileNameWithoutExtension(InputPath);
        OutputPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(OutputPath) ?? "",
            fileName + "_cropped.pdf");
    }

    [RelayCommand]
    private void SelectOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "保存裁剪后的PDF",
            FileName = System.IO.Path.GetFileName(OutputPath)
        };
        if (!string.IsNullOrEmpty(OutputPath))
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(OutputPath);
        if (dialog.ShowDialog() == true) OutputPath = dialog.FileName;
    }

    [RelayCommand]
    private void ApplySameMargin()
    {
        if (SameMarginForAll)
        {
            LeftMargin = TopMargin;
            RightMargin = LeftMargin;
            BottomMargin = TopMargin;
        }
    }

    [RelayCommand]
    private async Task CropAsync()
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
        if (LeftMargin < 0 || RightMargin < 0 || TopMargin < 0 || BottomMargin < 0)
        {
            System.Windows.MessageBox.Show("边距不能为负数", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsCropping = true;
        var result = await RunOperationAsync("裁剪", async (ct, prog) =>
            await _pdfService.CropPdfAsync(InputPath, OutputPath,
                LeftMargin, TopMargin, RightMargin, BottomMargin, prog, ct));
        ShowResult("裁剪", result);
    }

    [RelayCommand]
    private void CancelCrop() { }
}
